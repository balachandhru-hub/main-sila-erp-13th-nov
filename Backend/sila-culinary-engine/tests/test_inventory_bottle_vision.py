import asyncio
import base64
import io

import pytest
from PIL import Image, ImageDraw

from app.engines.bottle_vision import (
    analyze_bottle,
    estimate_fill_cv,
    load_bottle_image,
    parse_pack_size_ml,
)

LIQUIDS = {
    "gin_clear_blue": (70, 120, 200),
    "whisky_amber": (176, 98, 20),
    "wine_dark": (90, 20, 40),
    "green_glass": (40, 110, 60),
}


def synthetic_bottle(fill: float, liquid=(176, 98, 20), label=True, size=(360, 720), background=(236, 238, 241)) -> bytes:
    w, h = size
    image = Image.new("RGB", size, background)
    draw = ImageDraw.Draw(image)
    cx = w // 2
    body_w, neck_w = 150, 46
    cap_top, neck_top, shoulder_top, body_top, base = 40, 90, 230, 300, 670

    def silhouette(color):
        draw.rounded_rectangle([cx - neck_w // 2, cap_top, cx + neck_w // 2, shoulder_top + 10], 6, fill=color)
        draw.polygon([(cx - neck_w // 2, shoulder_top), (cx + neck_w // 2, shoulder_top),
                      (cx + body_w // 2, body_top), (cx - body_w // 2, body_top)], fill=color)
        draw.rounded_rectangle([cx - body_w // 2, body_top - 2, cx + body_w // 2, base], 10, fill=color)

    silhouette((205, 212, 218))
    full_row = shoulder_top + 20
    level = int(base - 6 - fill * (base - 6 - full_row))
    liquid_layer = Image.new("RGB", size, background)
    ld = ImageDraw.Draw(liquid_layer)
    ld.rectangle([0, level, w, base - 4], fill=liquid)
    mask = Image.new("L", size, 0)
    md = ImageDraw.Draw(mask)
    md.polygon([(cx - neck_w // 2 + 4, shoulder_top), (cx + neck_w // 2 - 4, shoulder_top),
                (cx + body_w // 2 - 4, body_top), (cx - body_w // 2 + 4, body_top)], fill=255)
    md.rounded_rectangle([cx - body_w // 2 + 4, body_top - 2, cx + body_w // 2 - 4, base - 4], 8, fill=255)
    md.rectangle([cx - neck_w // 2 + 4, shoulder_top - 40, cx + neck_w // 2 - 4, shoulder_top], fill=255)
    image.paste(liquid_layer, (0, 0), Image.composite(mask, Image.new("L", size, 0), mask))
    image_np = image.load()
    for y in range(h):
        for x in range(w):
            if mask.getpixel((x, y)) and y < level:
                image_np[x, y] = (205, 212, 218)
    draw = ImageDraw.Draw(image)
    draw.rounded_rectangle([cx - neck_w // 2, cap_top, cx + neck_w // 2, cap_top + 40], 4, fill=(30, 30, 30))
    if label:
        draw.rectangle([cx - 44, 420, cx + 44, 560], fill=(245, 240, 225), outline=(60, 40, 20), width=3)
        draw.rectangle([cx - 40, 450, cx + 40, 470], fill=(120, 20, 20))
    buf = io.BytesIO()
    image.save(buf, format="JPEG", quality=92)
    return buf.getvalue()


@pytest.mark.parametrize("name,color", list(LIQUIDS.items()))
@pytest.mark.parametrize("fill", [0.25, 0.5, 0.75])
def test_cv_estimates_fill_within_tolerance(name, color, fill):
    estimate = estimate_fill_cv(load_bottle_image(synthetic_bottle(fill, color)))
    assert estimate.bottle_detected
    assert estimate.fill_ratio is not None
    assert abs(estimate.fill_ratio - fill) <= 0.09, f"{name}: expected {fill}, got {estimate.fill_ratio}"
    assert estimate.confidence >= 0.4


def test_full_bottle_reads_as_sealed():
    result = asyncio.run(analyze_bottle(synthetic_bottle(1.0), 750, "WHISKY 750ML", None, "image/jpeg"))
    assert result.fill_ratio is not None and result.fill_ratio >= 0.93
    assert result.nominal_ml == 750


def test_consumed_ml_for_750ml_bottle():
    result = asyncio.run(analyze_bottle(synthetic_bottle(0.4), 750, None, None, "image/jpeg"))
    assert result.is_opened is True and result.seal_status == "OPENED"
    assert result.remaining_ml == pytest.approx(750 * result.fill_ratio, abs=0.6)
    assert result.consumed_ml == pytest.approx(750 - result.remaining_ml, abs=0.6)
    assert abs(result.consumed_ml - 450) <= 70


def test_pack_size_is_read_from_description_when_missing():
    result = asyncio.run(analyze_bottle(synthetic_bottle(0.5), None, "VODKA GREY GOOSE 70CL", None, "image/jpeg"))
    assert result.nominal_ml == 700
    assert not any("Pack size unknown" in w for w in result.warnings)


def test_unknown_pack_size_defaults_and_warns():
    result = asyncio.run(analyze_bottle(synthetic_bottle(0.5), None, "HOUSE POUR", None, "image/jpeg"))
    assert result.nominal_ml == 750
    assert any("Pack size unknown" in w for w in result.warnings)
    assert result.needs_manual_review


def test_blank_image_is_not_a_bottle():
    buf = io.BytesIO()
    Image.new("RGB", (300, 500), (230, 230, 230)).save(buf, format="PNG")
    result = asyncio.run(analyze_bottle(buf.getvalue(), 750, None, None, "image/png"))
    assert result.fill_ratio is None
    assert result.needs_manual_review
    assert result.remaining_ml is None


def test_rejects_non_image_and_empty():
    with pytest.raises(ValueError):
        load_bottle_image(b"")
    with pytest.raises(ValueError):
        load_bottle_image(b"not an image at all")


@pytest.mark.parametrize("text,expected", [
    ("GIN BOMBAY SAPPHIRE 750ML", 750), ("Vodka 70 cl", 700), ("Whisky 1.75 L", 1750),
    ("Wine 0,75 L", 750), ("Mini 50ML", 50), ("Chicken breast 1 KG", None), (None, None),
])
def test_parse_pack_size(text, expected):
    assert parse_pack_size_ml(text) == expected


def test_endpoint_roundtrip():
    from fastapi.testclient import TestClient
    from app.main import app

    client = TestClient(app)
    body = {"image_base64": base64.b64encode(synthetic_bottle(0.5)).decode(), "mime_type": "image/jpeg",
            "nominal_ml": 750, "description": "WHISKY"}
    response = client.post("/v1/inventory/bottle-level", json=body)
    assert response.status_code == 200
    data = response.json()
    assert data["method"] == "CV_HEURISTIC"
    assert abs(data["fill_ratio"] - 0.5) <= 0.09
    assert client.post("/v1/inventory/bottle-level", json={"image_base64": "###"}).status_code == 400


def _llm(monkeypatch, ratio, confidence=0.9):
    from app.engines import bottle_vision as bv

    async def fake(image_bytes, mime, nominal_ml, description, cfg):
        return bv.BottleVisionLlmPayload(bottle_detected=True, is_opened=ratio < 0.97, fill_ratio=ratio,
                                         confidence=confidence, label_text="Test Label"), "LOCAL", "fake-vision"

    monkeypatch.setattr(bv, "estimate_fill_llm", fake)
    from app.schemas.recipe_ai import ProviderRuntimeConfig
    return ProviderRuntimeConfig(tenant_id="t1")


def test_llm_and_cv_agree_blend(monkeypatch):
    cfg = _llm(monkeypatch, 0.52)
    result = asyncio.run(analyze_bottle(synthetic_bottle(0.5), 750, None, cfg, "image/jpeg"))
    assert result.method == "LLM_VISION+CV"
    assert result.provider == "LOCAL" and result.label_text == "Test Label"
    assert abs(result.fill_ratio - 0.5) <= 0.08
    assert result.confidence >= 0.8


def test_llm_and_cv_disagree_flags_review(monkeypatch):
    cfg = _llm(monkeypatch, 0.9)
    result = asyncio.run(analyze_bottle(synthetic_bottle(0.3), 750, None, cfg, "image/jpeg"))
    assert result.needs_manual_review
    assert any("disagree" in w for w in result.warnings)
    assert result.confidence <= 0.55


def test_provider_failure_keeps_cv_result():
    from app.schemas.recipe_ai import ProviderRuntimeConfig
    cfg = ProviderRuntimeConfig(tenant_id="t1", local_enabled=False)
    result = asyncio.run(analyze_bottle(synthetic_bottle(0.5), 750, None, cfg, "image/jpeg"))
    assert result.method == "CV_HEURISTIC"
    assert any("Vision AI provider" in w for w in result.notices)
    assert not result.needs_manual_review

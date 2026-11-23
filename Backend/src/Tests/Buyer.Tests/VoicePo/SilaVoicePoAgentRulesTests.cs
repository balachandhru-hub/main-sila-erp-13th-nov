using Buyer.Application.Features.Shared;
using Buyer.Domain.Dtos;

namespace Buyer.Tests.VoicePo
{
    /// <summary>
    /// Pure unit tests for Voice PO deterministic agent rules: confirmation, default phrases, draft validation.
    /// </summary>
    public class SilaVoicePoAgentRulesTests
    {
        [Theory]
        [InlineData("confirm purchase order")]
        [InlineData("Please confirm the purchase order")]
        [InlineData("I confirm this purchase order")]
        [InlineData("confirm and create")]
        [InlineData("create the purchase order")]
        public void Explicit_confirmation_phrases_are_accepted(string phrase)
        {
            Assert.True(SilaVoicePoAgentRules.IsExplicitConfirmation(phrase));
            Assert.False(SilaVoicePoAgentRules.RejectsConfirmation(phrase));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("yes")]
        [InlineData("ok")]
        [InlineData("okay")]
        [InlineData("sure")]
        [InlineData("yep")]
        [InlineData("go")]
        [InlineData("do it")]
        public void Yes_ok_and_silence_are_rejected(string? phrase)
        {
            Assert.True(SilaVoicePoAgentRules.RejectsConfirmation(phrase));
            Assert.False(SilaVoicePoAgentRules.IsExplicitConfirmation(phrase));
        }

        [Theory]
        [InlineData("go with default")]
        [InlineData("use my defaults")]
        [InlineData("apply default")]
        [InlineData("use preferred supplier")]
        public void Go_with_default_phrases_are_detected(string phrase)
        {
            Assert.True(SilaVoicePoAgentRules.IsGoWithDefault(phrase));
            Assert.Equal(VoicePoIntent.GoWithDefault, SilaVoicePoAgentRules.ParseOrderIntent(phrase).Intent);
        }

        [Theory]
        [InlineData("double qty")]
        [InlineData("double the quantity")]
        [InlineData("make it double")]
        public void Double_quantity_phrases_are_detected(string phrase)
        {
            Assert.True(SilaVoicePoAgentRules.IsDoubleQty(phrase));
            Assert.Equal(VoicePoIntent.DoubleQuantity, SilaVoicePoAgentRules.ParseOrderIntent(phrase).Intent);
        }

        [Fact]
        public void Parse_order_intent_extracts_quantity_uom_and_material()
        {
            VoicePoParseResult result = SilaVoicePoAgentRules.ParseOrderIntent("order 10 kg chicken breast");

            Assert.Equal(VoicePoIntent.AddLine, result.Intent);
            Assert.Equal(10m, result.Quantity);
            Assert.Equal("KG", result.UnitOfMeasure);
            Assert.Equal("chicken breast", result.MaterialHint);
        }

        [Fact]
        public void Uncertain_transcript_is_flagged_for_low_confidence_or_silence()
        {
            Assert.True(SilaVoicePoAgentRules.IsUncertainTranscript(null, null));
            Assert.True(SilaVoicePoAgentRules.IsUncertainTranscript("chicken", 0.2m));
            Assert.True(SilaVoicePoAgentRules.IsUncertainTranscript("chicken", 0.9m, "FAILED"));
            Assert.False(SilaVoicePoAgentRules.IsUncertainTranscript("order 5 kg rice", 0.9m));
        }

        [Fact]
        public void Draft_validation_requires_resolved_material_and_supplier()
        {
            SilaVoicePoDraftDto draft = new()
            {
                Lines =
                {
                    new SilaVoicePoDraftLineDto
                    {
                        LineNumber = 1,
                        Quantity = 2,
                        Resolved = false,
                        Description = "mystery item"
                    }
                }
            };

            SilaVoicePoDraftValidation validation = SilaVoicePoAgentRules.ValidateDraft(draft);

            Assert.NotEqual(SilaVoicePoAgentRules.VALIDATION_OK, validation.Status);
            Assert.Contains(validation.Errors, e => e.Contains("material", StringComparison.OrdinalIgnoreCase));
            Assert.Contains(validation.Errors, e => e.Contains("supplier", StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        public void Draft_validation_passes_for_resolved_stock_line_without_cost_center()
        {
            SilaVoicePoDraftDto draft = new()
            {
                Lines =
                {
                    new SilaVoicePoDraftLineDto
                    {
                        LineNumber = 1,
                        MaterialCode = "MAT-001",
                        Description = "Rice",
                        Quantity = 5,
                        UnitOfMeasure = "KG",
                        SupplierId = Guid.NewGuid(),
                        InventoryType = SilaUserPurchasingProfileRules.INVENTORY_STOCK,
                        Resolved = true
                    }
                }
            };

            SilaVoicePoDraftValidation validation = SilaVoicePoAgentRules.ValidateDraft(draft);

            Assert.Equal(SilaVoicePoAgentRules.VALIDATION_OK, validation.Status);
            Assert.Empty(validation.Errors);
        }

        [Fact]
        public void Consumable_draft_requires_account_assignment()
        {
            SilaVoicePoDraftDto draft = new()
            {
                Lines =
                {
                    new SilaVoicePoDraftLineDto
                    {
                        LineNumber = 1,
                        MaterialCode = "CONS-1",
                        Description = "Cleaning spray",
                        Quantity = 1,
                        SupplierId = Guid.NewGuid(),
                        InventoryType = SilaUserPurchasingProfileRules.INVENTORY_CONSUMABLE,
                        Resolved = true
                    }
                }
            };

            SilaVoicePoDraftValidation validation = SilaVoicePoAgentRules.ValidateDraft(draft);

            Assert.Equal(SilaVoicePoAgentRules.VALIDATION_NEEDS_CLARIFICATION, validation.Status);
            Assert.Contains(validation.Errors, e => e.Contains("account assignment", StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        public void Go_with_default_applies_profile_without_forcing_stock_cost_center()
        {
            SilaVoicePoDraftDto draft = new()
            {
                Lines =
                {
                    new SilaVoicePoDraftLineDto
                    {
                        LineNumber = 1,
                        MaterialCode = "MAT-001",
                        Quantity = 1,
                        InventoryType = SilaUserPurchasingProfileRules.INVENTORY_STOCK,
                        Resolved = true,
                        SupplierId = Guid.NewGuid()
                    }
                }
            };
            SilaUserPurchasingDefaultsDto defaults = new()
            {
                CompanyCode = "1000",
                PlantCode = "P001",
                CostCenter = "CC1",
                GlAccount = "GL1",
                AccountAssignmentCategory = "K",
                PreferredSupplierId = Guid.NewGuid(),
                PreferredSupplierCode = "V001",
                InventoryType = SilaUserPurchasingProfileRules.INVENTORY_STOCK,
                RequiresAccountAssignment = false
            };

            SilaVoicePoAgentRules.ApplyGoWithDefault(draft, defaults);

            Assert.Equal("1000", draft.CompanyCode);
            Assert.Equal("P001", draft.PlantCode);
            Assert.Null(draft.Lines[0].CostCenter);
            Assert.Null(draft.Lines[0].GlAccount);
        }

        [Fact]
        public void Double_and_remove_line_mutate_draft()
        {
            SilaVoicePoDraftDto draft = new()
            {
                Lines =
                {
                    new SilaVoicePoDraftLineDto { LineNumber = 1, MaterialCode = "A", Quantity = 2, Resolved = true, SupplierId = Guid.NewGuid() },
                    new SilaVoicePoDraftLineDto { LineNumber = 2, MaterialCode = "B", Description = "Butter", Quantity = 3, Resolved = true, SupplierId = Guid.NewGuid() }
                }
            };

            SilaVoicePoAgentRules.DoubleQuantities(draft);
            Assert.Equal(4m, draft.Lines[0].Quantity);
            Assert.Equal(6m, draft.Lines[1].Quantity);

            Assert.True(SilaVoicePoAgentRules.RemoveLine(draft, "butter"));
            Assert.Single(draft.Lines);
            Assert.Equal("A", draft.Lines[0].MaterialCode);
            Assert.Equal(1, draft.Lines[0].LineNumber);
        }

        [Fact]
        public void Env_url_helpers_do_not_hardcode_secrets()
        {
            Assert.Equal("SILA_VOICE_PO_LLM_URL", SilaVoicePoAgentRules.ENV_LLM_URL);
            Assert.Equal("SILA_VOICE_PO_STT_URL", SilaVoicePoAgentRules.ENV_STT_URL);
            Assert.Equal("SILA_VOICE_PO_TTS_URL", SilaVoicePoAgentRules.ENV_TTS_URL);
        }
    }
}

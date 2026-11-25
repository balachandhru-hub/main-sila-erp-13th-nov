# Isolated SILA Voice Engine. Does not replace Buyer.API or python-backend OCR.
FROM python:3.12-slim

WORKDIR /app
COPY requirements.txt .
RUN pip install --no-cache-dir -r requirements.txt
COPY app ./app
COPY appsettings.json .

ENV VOICE_HOST=0.0.0.0
ENV VOICE_PORT=8012
EXPOSE 8012

HEALTHCHECK --interval=30s --timeout=5s --start-period=20s --retries=3 \
  CMD python -c "import urllib.request; urllib.request.urlopen('http://127.0.0.1:8012/health')" || exit 1

CMD ["uvicorn", "app.main:app", "--host", "0.0.0.0", "--port", "8012", "--timeout-keep-alive", "30"]

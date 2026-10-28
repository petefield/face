
FROM python:3.12-slim

RUN apt-get update && \
    apt-get install -y --no-install-recommends \
        alsa-utils \
        libasound2-plugins && \
    rm -rf /var/lib/apt/lists/*

RUN pip install --no-cache-dir \
    piper-tts==1.8.0 \
    fastapi \
    uvicorn

WORKDIR /app

COPY app.py .

EXPOSE 5000

CMD ["uvicorn", "app:app", "--host", "0.0.0.0", "--port", "5000"]

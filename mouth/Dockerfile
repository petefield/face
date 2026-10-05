FROM python:3.12-slim

RUN apt-get update && \
    apt-get install -y --no-install-recommends alsa-utils && \
    rm -rf /var/lib/apt/lists/*

RUN pip install --no-cache-dir \
    piper-tts==1.8.0 \
    fastapi \
    uvicorn

RUN mkdir -p /voices && \
    python -m piper.download_voices \
      --data-dir /voices \
      en_GB-alan-medium

WORKDIR /app

COPY app.py .

ENV PIPER_MODEL=/voices/en_GB-semaine-medium.onnx
ENV AUDIO_DEVICE=default

EXPOSE 5000

CMD ["uvicorn", "app:app", "--host", "0.0.0.0", "--port", "5000"]

FROM python:3.12-slim

RUN apt-get update && \
    apt-get install -y --no-install-recommends \
        espeak-ng \
        pulseaudio-utils && \
    rm -rf /var/lib/apt/lists/*

RUN pip install --no-cache-dir fastapi uvicorn

WORKDIR /app
COPY app.py .

EXPOSE 5000

CMD ["uvicorn", "app:app", "--host", "0.0.0.0", "--port", "5000"]
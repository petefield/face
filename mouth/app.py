
import io
import os
import subprocess
import threading
import wave

from fastapi import FastAPI, HTTPException
from pydantic import BaseModel, Field
from piper import PiperVoice, SynthesisConfig

MODEL = os.getenv(
    "PIPER_MODEL",
    "/voices/en_GB-semaine-medium.onnx"
)

AUDIO_DEVICE = os.getenv("AUDIO_DEVICE", "default")
SPEECH_SPEED = float(os.getenv("SPEECH_SPEED", "0.85"))

app = FastAPI()
speak_lock = threading.Lock()

# Load Piper once, not for every request
voice = PiperVoice.load(MODEL)


class SpeakRequest(BaseModel):
    text: str
    speed: float | None = Field(
        default=None,
        gt=0,
        le=3
    )


@app.get("/health")
def health():
    return {
        "status": "ok",
        "engine": "piper",
        "model": os.path.basename(MODEL)
    }


@app.post("/speak")
def speak(request: SpeakRequest):
    text = request.text.strip()

    if not text:
        raise HTTPException(
            status_code=400,
            detail="Text cannot be empty"
        )

    speed = request.speed or SPEECH_SPEED

    with speak_lock:
        # Generate WAV in memory
        buffer = io.BytesIO()

        config = SynthesisConfig(
            length_scale=1.0 / speed
        )

        with wave.open(buffer, "wb") as wav_file:
            voice.synthesize_wav(
                text,
                wav_file,
                syn_config=config
            )

        # Play WAV through ALSA
        result = subprocess.run(
            ["aplay", "-q", "-D", AUDIO_DEVICE],
            input=buffer.getvalue(),
            capture_output=True
        )

        if result.returncode != 0:
            raise HTTPException(
                status_code=500,
                detail=result.stderr.decode(
                    errors="replace"
                )
            )

    return {
        "spoken": True,
        "text": text,
        "engine": "piper",
        "speed": speed
    }

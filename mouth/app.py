import os
import subprocess
import threading

from fastapi import FastAPI, HTTPException
from pydantic import BaseModel
from piper import PiperVoice
from piper import SynthesisConfig



MODEL = os.getenv(
    "PIPER_MODEL",
    "/voices/en_GB-alan-medium.onnx"
)

AUDIO_DEVICE = os.getenv(
    "AUDIO_DEVICE",
    "default"
)

SPEECH_SPEED = os.getenv(
    "SPEECH_SPEED",
    0.85
)

config = SynthesisConfig(
    length_scale=SPEECH_SPEED,
    speaker_id=3,

)

print(f"Loading Piper model: {MODEL}")

voice = PiperVoice.load(MODEL)

print("Piper model loaded")

app = FastAPI()

speak_lock = threading.Lock()


class SpeakRequest(BaseModel):
    text: str


@app.get("/health")
def health():
    return {
        "status": "ok",
        "model": MODEL,
        "audio_device": AUDIO_DEVICE
    }


@app.post("/speak")
def speak(request: SpeakRequest):

    text = request.text.strip()

    if not text:
        raise HTTPException(
            status_code=400,
            detail="Text cannot be empty"
        )

    with speak_lock:

        process = subprocess.Popen(
            [
                "aplay",
                "-q",
                "-D", AUDIO_DEVICE,
                "-f", "S16_LE",
                "-r", str(voice.config.sample_rate),
                "-c", "1"
            ],
            stdin=subprocess.PIPE
        )

        try:
            for chunk in voice.synthesize(text, syn_config=config):
                process.stdin.write(
                    chunk.audio_int16_bytes
                )

            process.stdin.close()

            result = process.wait()

            if result != 0:
                raise RuntimeError(
                    f"aplay exited with code {result}"
                )

        except Exception:

            if process.poll() is None:
                process.kill()

            raise

    return {
        "spoken": True,
        "text": text
    }

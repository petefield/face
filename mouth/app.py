import subprocess
import threading

from fastapi import FastAPI, HTTPException
from pydantic import BaseModel

app = FastAPI()

speak_lock = threading.Lock()


class SpeakRequest(BaseModel):
    text: str
    speed: int = 180


@app.get("/health")
def health():
    return {"status": "ok"}


@app.post("/speak")
def speak(request: SpeakRequest):

    text = request.text.strip()

    if not text:
        raise HTTPException(
            status_code=400,
            detail="Text cannot be empty"
        )

    with speak_lock:

        espeak = subprocess.Popen(
            [
                "espeak-ng",
                "-v", "en-gb",
                "-s", str(request.speed),
                "--stdout",
                text
            ],
            stdout=subprocess.PIPE
        )

        player = subprocess.Popen(
            ["paplay"],
            stdin=espeak.stdout
        )

        espeak.stdout.close()

        result = player.wait()
        espeak.wait()

        if result != 0:
            raise RuntimeError(
                f"paplay exited with code {result}"
            )

    return {
        "spoken": True,
        "text": text
    }
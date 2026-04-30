from __future__ import annotations

import os


BOT_TOKEN = os.getenv("TELEGRAM_BOT_TOKEN", "8268119707:AAF_jZsbhoChaJf-VZaKETaFnb9T9V1dc-g")
PAYMENT_PROVIDER_TOKEN = os.getenv("TELEGRAM_PROVIDER_TOKEN", "1744374395:TEST:e622a5a9c69996bd9809")
BOT_USERNAME = os.getenv("TELEGRAM_BOT_USERNAME", "iktrainingbot")
API_BASE_URL = os.getenv("POWERFITNESS_API_URL", "http://localhost:5004")
ALLOW_SELF_SIGNED_HTTPS = os.getenv("POWERFITNESS_ALLOW_SELF_SIGNED", "0") == "1"

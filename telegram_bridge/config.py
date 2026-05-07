from __future__ import annotations

import os


BOT_TOKEN = os.getenv("TELEGRAM_BOT_TOKEN", "")
PAYMENT_PROVIDER_TOKEN = os.getenv("TELEGRAM_PROVIDER_TOKEN", "")
BOT_USERNAME = os.getenv("TELEGRAM_BOT_USERNAME", "iktrainingbot")
API_BASE_URL = os.getenv("POWERFITNESS_API_URL", "http://localhost:5004")
ALLOW_SELF_SIGNED_HTTPS = os.getenv("POWERFITNESS_ALLOW_SELF_SIGNED", "0") == "1"

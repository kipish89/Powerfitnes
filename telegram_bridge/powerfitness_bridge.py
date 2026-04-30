from __future__ import annotations

import json
import ssl
import urllib.error
import urllib.request
from dataclasses import dataclass
from decimal import Decimal

from config import ALLOW_SELF_SIGNED_HTTPS, API_BASE_URL


@dataclass(slots=True)
class TelegramUserPayload:
    ticket_id: str
    phone_number: str
    telegram_chat_id: str
    first_name: str
    last_name: str


@dataclass(slots=True)
class PaymentPayload:
    user_id: str
    product_code: str
    product_type: str
    amount: Decimal
    payment_id: str | None = None
    currency: str = "RUB"


@dataclass(slots=True)
class PurchaseIntentPayload:
    id: str
    user_id: str
    product_code: str
    product_type: str
    amount: Decimal
    status: str


def _build_ssl_context() -> ssl.SSLContext | None:
    if not API_BASE_URL.startswith("https://") or not ALLOW_SELF_SIGNED_HTTPS:
        return None

    context = ssl.create_default_context()
    context.check_hostname = False
    context.verify_mode = ssl.CERT_NONE
    return context


def _request_json(path: str, method: str = "GET", payload: dict | None = None) -> dict:
    data = None if payload is None else json.dumps(payload, ensure_ascii=False).encode("utf-8")
    request = urllib.request.Request(
        url=f"{API_BASE_URL}{path}",
        data=data,
        headers={"Content-Type": "application/json"},
        method=method,
    )

    try:
        with urllib.request.urlopen(request, context=_build_ssl_context()) as response:
            raw = response.read().decode("utf-8")
            return json.loads(raw) if raw else {}
    except urllib.error.HTTPError as exc:
        body = exc.read().decode("utf-8", errors="ignore")
        raise RuntimeError(f"API error {exc.code}: {body}") from exc
    except urllib.error.URLError as exc:
        raise RuntimeError(f"Cannot reach API at {API_BASE_URL}: {exc.reason}") from exc


def confirm_registration(payload: TelegramUserPayload) -> dict:
    return _request_json(
        "/api/auth/telegram/confirm",
        method="POST",
        payload={
            "ticketId": payload.ticket_id,
            "phoneNumber": payload.phone_number,
            "telegramChatId": payload.telegram_chat_id,
            "firstName": payload.first_name,
            "lastName": payload.last_name,
        },
    )


def get_purchase_intent(payment_id: str) -> PurchaseIntentPayload:
    payload = _request_json(f"/api/purchases/{payment_id}")
    return PurchaseIntentPayload(
        id=payload["id"],
        user_id=payload["userId"],
        product_code=payload["productCode"],
        product_type=payload["productType"],
        amount=Decimal(str(payload["amount"])),
        status=payload["status"],
    )


def confirm_payment(payload: PaymentPayload) -> dict:
    body = {
        "userId": payload.user_id,
        "productCode": payload.product_code,
        "productType": payload.product_type,
        "amount": float(payload.amount),
        "currency": payload.currency,
    }

    if payload.payment_id:
        body["paymentId"] = payload.payment_id

    return _request_json(
        "/api/bot/payment-confirmation",
        method="POST",
        payload=body,
    )

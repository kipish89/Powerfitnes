from __future__ import annotations

import asyncio
import logging
from dataclasses import dataclass
from decimal import Decimal

from telegram import (
    InlineKeyboardButton,
    InlineKeyboardMarkup,
    KeyboardButton,
    LabeledPrice,
    ReplyKeyboardMarkup,
    ReplyKeyboardRemove,
    Update,
)
from telegram.ext import (
    Application,
    CallbackQueryHandler,
    CommandHandler,
    ContextTypes,
    MessageHandler,
    PreCheckoutQueryHandler,
    filters,
)

from config import API_BASE_URL, BOT_TOKEN, BOT_USERNAME, PAYMENT_PROVIDER_TOKEN
from powerfitness_bridge import (
    PaymentPayload,
    TelegramUserPayload,
    confirm_payment,
    confirm_registration,
    get_purchase_intent,
)


logging.basicConfig(
    format="%(asctime)s | %(levelname)s | %(name)s | %(message)s",
    level=logging.INFO,
)
logger = logging.getLogger("powerfitness_bot")


PLANS: dict[str, dict[str, object]] = {
    "gym-3m": {"title": "Абонемент на 3 месяца", "price": Decimal("4500"), "type": "membership"},
    "gym-6m": {"title": "Абонемент на 6 месяцев", "price": Decimal("7800"), "type": "membership"},
    "gym-12m": {"title": "Абонемент на 12 месяцев", "price": Decimal("12900"), "type": "membership"},
    "pro-1m": {"title": "PowerFitness Pro", "price": Decimal("990"), "type": "pro"},
}


@dataclass(slots=True)
class PendingRegistration:
    ticket_id: str


def _plans_keyboard() -> InlineKeyboardMarkup:
    return InlineKeyboardMarkup(
        [
            [InlineKeyboardButton("3 месяца", callback_data="buy:gym-3m")],
            [InlineKeyboardButton("6 месяцев", callback_data="buy:gym-6m")],
            [InlineKeyboardButton("12 месяцев", callback_data="buy:gym-12m")],
            [InlineKeyboardButton("PowerFitness Pro", callback_data="buy:pro-1m")],
        ]
    )


async def start_command(update: Update, context: ContextTypes.DEFAULT_TYPE) -> None:
    payload = context.args[0] if context.args else ""

    if payload.startswith("register_"):
        ticket_hex = payload.removeprefix("register_")
        ticket_id = _normalize_guid(ticket_hex)
        context.user_data["pending_registration"] = PendingRegistration(ticket_id=ticket_id)

        contact_keyboard = ReplyKeyboardMarkup(
            [[KeyboardButton("Поделиться телефоном", request_contact=True)]],
            resize_keyboard=True,
            one_time_keyboard=True,
        )
        await update.effective_message.reply_text(
            "Чтобы завершить регистрацию в PowerFitness, нажми кнопку и отправь свой номер телефона.",
            reply_markup=contact_keyboard,
        )
        return

    if payload.startswith("buy_"):
        payment_id = _parse_payment_payload(payload)
        if payment_id:
            try:
                purchase = get_purchase_intent(payment_id)
            except Exception as exc:  # noqa: BLE001
                logger.exception("Purchase deep link failed")
                await update.effective_message.reply_text(
                    f"Не удалось открыть выбранную покупку: {exc}",
                    reply_markup=ReplyKeyboardRemove(),
                )
                return

            context.user_data["user_id"] = purchase.user_id
            context.user_data["pending_payment_id"] = payment_id
            await update.effective_message.reply_text(
                f"Открываю оплату: {PLANS[purchase.product_code]['title']}.",
                reply_markup=ReplyKeyboardRemove(),
            )
            await _start_purchase(update, context, purchase.product_code, payment_id=payment_id)
            return

    await update.effective_message.reply_text(
        "Бот PowerFitness готов. Команды: /plans, /buy_pro, /help",
        reply_markup=ReplyKeyboardRemove(),
    )


async def help_command(update: Update, context: ContextTypes.DEFAULT_TYPE) -> None:
    await update.effective_message.reply_text(
        "Сценарий работы:\n"
        "1. Пользователь заходит из приложения.\n"
        "2. Подтверждает телефон.\n"
        "3. Бот подтверждает регистрацию через API.\n"
        "4. Оплата абонемента проходит через Telegram invoice."
    )


async def plans_command(update: Update, context: ContextTypes.DEFAULT_TYPE) -> None:
    await update.effective_message.reply_text(
        "Выбери продукт PowerFitness:",
        reply_markup=_plans_keyboard(),
    )


async def buy_pro_command(update: Update, context: ContextTypes.DEFAULT_TYPE) -> None:
    await _start_purchase(update, context, "pro-1m")


async def text_fallback_handler(update: Update, context: ContextTypes.DEFAULT_TYPE) -> None:
    if update.effective_message is None:
        return

    await update.effective_message.reply_text(
        "Я на связи. Используй /start, /plans или зайди в бота по ссылке из приложения PowerFitness."
    )


async def contact_handler(update: Update, context: ContextTypes.DEFAULT_TYPE) -> None:
    pending: PendingRegistration | None = context.user_data.get("pending_registration")
    contact = update.effective_message.contact if update.effective_message else None

    if pending is None or contact is None:
        if update.effective_message is not None:
            await update.effective_message.reply_text("Сначала открой бота из приложения PowerFitness.")
        return

    if update.effective_user is None or update.effective_chat is None:
        return

    payload = TelegramUserPayload(
        ticket_id=pending.ticket_id,
        phone_number=contact.phone_number,
        telegram_chat_id=str(update.effective_chat.id),
        first_name=update.effective_user.first_name or "",
        last_name=update.effective_user.last_name or "",
    )

    try:
        user = confirm_registration(payload)
    except Exception as exc:  # noqa: BLE001
        logger.exception("Registration confirmation failed")
        await update.effective_message.reply_text(
            f"Не удалось подтвердить регистрацию: {exc}",
            reply_markup=ReplyKeyboardRemove(),
        )
        return

    context.user_data["user_id"] = user["id"]
    context.user_data.pop("pending_registration", None)

    await update.effective_message.reply_text(
        "Регистрация подтверждена. Теперь можно покупать абонементы и PowerFitness Pro.",
        reply_markup=ReplyKeyboardRemove(),
    )
    await update.effective_message.reply_text("Открой каталог:", reply_markup=_plans_keyboard())


async def buy_callback(update: Update, context: ContextTypes.DEFAULT_TYPE) -> None:
    query = update.callback_query
    if query is None or query.data is None:
        return

    await query.answer()
    _, product_code = query.data.split(":", 1)
    await _start_purchase(update, context, product_code, use_callback_message=True)


async def precheckout_handler(update: Update, context: ContextTypes.DEFAULT_TYPE) -> None:
    query = update.pre_checkout_query
    if query is None:
        return

    await query.answer(ok=True)


async def successful_payment_handler(update: Update, context: ContextTypes.DEFAULT_TYPE) -> None:
    if update.effective_message is None or update.effective_message.successful_payment is None:
        return

    payment_info = update.effective_message.successful_payment

    try:
        payment_id, user_id, product_code, product_type = _parse_invoice_payload(payment_info.invoice_payload)
    except ValueError as exc:
        await update.effective_message.reply_text(f"Не удалось обработать платёж: {exc}")
        return

    try:
        confirm_payment(
            PaymentPayload(
                payment_id=payment_id,
                user_id=user_id,
                product_code=product_code,
                product_type=product_type,
                amount=Decimal(payment_info.total_amount) / Decimal("100"),
            )
        )
    except Exception as exc:  # noqa: BLE001
        logger.exception("Payment confirmation failed")
        await update.effective_message.reply_text(f"Платёж получен, но API не обновился: {exc}")
        return

    await update.effective_message.reply_text(
        f"Платёж за '{PLANS[product_code]['title']}' успешно подтверждён. "
        "Статус в приложении обновится после следующего запроса к API."
    )


async def _start_purchase(
    update: Update,
    context: ContextTypes.DEFAULT_TYPE,
    product_code: str,
    payment_id: str | None = None,
    use_callback_message: bool = False,
) -> None:
    user_id = context.user_data.get("user_id")
    plan = PLANS.get(product_code)
    target = update.callback_query.message if use_callback_message and update.callback_query else update.effective_message

    if target is None:
        return

    if plan is None:
        await target.reply_text("Неизвестный продукт.")
        return

    if not user_id:
        await target.reply_text("Сначала заверши регистрацию через приложение и отправь контакт.")
        return

    if PAYMENT_PROVIDER_TOKEN:
        price = int(Decimal(plan["price"]) * 100)
        await context.bot.send_invoice(
            chat_id=target.chat_id,
            title=str(plan["title"]),
            description=f"Покупка продукта PowerFitness: {plan['title']}",
            payload=_build_invoice_payload(payment_id, str(user_id), product_code, str(plan["type"])),
            provider_token=PAYMENT_PROVIDER_TOKEN,
            currency="RUB",
            prices=[LabeledPrice(str(plan["title"]), price)],
        )
        return

    try:
        confirm_payment(
            PaymentPayload(
                payment_id=payment_id,
                user_id=str(user_id),
                product_code=product_code,
                product_type=str(plan["type"]),
                amount=Decimal(plan["price"]),
            )
        )
    except Exception as exc:  # noqa: BLE001
        logger.exception("Demo payment confirmation failed")
        await target.reply_text(f"Не удалось создать демо-платёж: {exc}")
        return

    await target.reply_text(
        f"Демо-оплата '{plan['title']}' подтверждена через API. "
        "Для реальных платежей должен быть задан TELEGRAM_PROVIDER_TOKEN."
    )


def _normalize_guid(ticket_hex: str) -> str:
    if len(ticket_hex) == 32:
        return (
            f"{ticket_hex[0:8]}-{ticket_hex[8:12]}-{ticket_hex[12:16]}-"
            f"{ticket_hex[16:20]}-{ticket_hex[20:32]}"
        )
    return ticket_hex


def _parse_payment_payload(payload: str) -> str | None:
    payment_hex = payload.removeprefix("buy_")
    payment_id = _normalize_guid(payment_hex)
    return payment_id if payment_id else None


def _build_invoice_payload(payment_id: str | None, user_id: str, product_code: str, product_type: str) -> str:
    if payment_id:
        return f"pay:{payment_id}:{product_code}:{product_type}"

    return f"usr:{user_id}:{product_code}:{product_type}"


def _parse_invoice_payload(payload: str) -> tuple[str | None, str, str, str]:
    parts = payload.split(":")
    if len(parts) != 4:
        raise ValueError("invalid invoice payload")

    mode, first, product_code, product_type = parts
    if mode == "pay":
        purchase = get_purchase_intent(first)
        return first, purchase.user_id, product_code, product_type

    if mode == "usr":
        return None, first, product_code, product_type

    raise ValueError("unknown invoice payload mode")


def main() -> None:
    if not BOT_TOKEN:
        raise RuntimeError("Set TELEGRAM_BOT_TOKEN before starting the bot.")

    try:
        asyncio.get_event_loop()
    except RuntimeError:
        loop = asyncio.new_event_loop()
        asyncio.set_event_loop(loop)

    application = (
        Application.builder()
        .token(BOT_TOKEN)
        .connect_timeout(30)
        .read_timeout(30)
        .write_timeout(30)
        .pool_timeout(30)
        .get_updates_connect_timeout(30)
        .get_updates_read_timeout(30)
        .get_updates_write_timeout(30)
        .get_updates_pool_timeout(30)
        .build()
    )
    application.add_handler(CommandHandler("start", start_command))
    application.add_handler(CommandHandler("help", help_command))
    application.add_handler(CommandHandler("plans", plans_command))
    application.add_handler(CommandHandler("buy_pro", buy_pro_command))
    application.add_handler(CallbackQueryHandler(buy_callback, pattern=r"^buy:"))
    application.add_handler(MessageHandler(filters.CONTACT, contact_handler))
    application.add_handler(MessageHandler(filters.TEXT & ~filters.COMMAND, text_fallback_handler))
    application.add_handler(PreCheckoutQueryHandler(precheckout_handler))
    application.add_handler(MessageHandler(filters.SUCCESSFUL_PAYMENT, successful_payment_handler))

    logger.info("Bot started as @%s using API %s", BOT_USERNAME, API_BASE_URL)
    application.run_polling(allowed_updates=Update.ALL_TYPES)


if __name__ == "__main__":
    main()

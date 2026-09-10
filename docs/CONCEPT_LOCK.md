# ۳۳ / 33 Label — Concept Lock v1.0

> وضعیت: **قفل کانسپت** — پیاده‌سازی فقط پس از تأیید صریح کارفرما  
> منبع UI: داکیومنت برند ۳۳ + پاسخ‌های قفل (۱۴۰۴)  
> Backend: ریپوی مستقل `Label33-` (بدون ارتباط با IntraMovie)

---

## ۱. کانسپت یک‌خطی

**۳۳** برند streetwear/editorial ایرانی است: لباس‌های روزمره آشنا، هویت متفاوت، و کاراکتر **Diyar** به‌عنوان نماد بصری — با حس کمپین مد، نه فروشگاه ژنریک.

---

## ۲. هویت برند

| مورد | مقدار قفل‌شده |
|------|----------------|
| نام نمایشی | **۳۳** |
| دیجیتال | `33label` |
| تگ‌لاین | **SAME CLOTHES. DIFFERENT MINDS.** |
| کاراکتر | **Diyar** (مرغ‌دریایی/پرستو مینیمال؛ ۳۳ در negative space) |
| محصولات فاز ۱ | T-Shirt، Hoodie، Sweatshirt، Jogger |
| شخصیت | Minimal / Premium / Editorial / Gen Z / Confident / Playful-sophisticated / Warm |
| ممنوع | کودکانه، کارتونی، streetwear ژنریک، رنگ‌پاشی، شرکتی، آینده‌نگر سرد |

---

## ۳. زبان (i18n)

- **پیش‌فرض: فارسی (fa)**
- دکمهٔ تغییر زبان در UI → **English (en)**
- تگ‌لاین و بخش‌های برند می‌توانند دوفرم داشته باشند؛ UI فروشگاهی کامل ترجمه می‌شود
- جهت: `fa` = RTL ، `en` = LTR
- ترجیح کاربر زبان در cookie/localStorage ذخیره می‌شود

---

## ۴. Intro

- فقط روی **ورود به Home با full page load**
- **Skip** همیشه可见
- منطق:
  - اولین ورود به Home در همان tab → پخش Intro
  - ناوبری داخلی برگشت به Home بدون reload کامل → بدون Replay (در حد امکان)
  - **رفرش کامل (F5 / reload)** → Intro دوباره پخش می‌شود
- حس: opening کمپین مد، نه لودینگ

---

## ۵. خرید و Checkout

- **Quick Add از Shop مجاز است** (افزودن سریع؛ اگر واریانت پیش‌فرض/تک‌سایز نباشد، باید سایز پیش‌فرض یا مودال خیلی کوتاه تعریف شود در پیاده‌سازی)
- **Checkout: یک صفحه ساده**
- **پرداخت فقط برای کاربر لاگین‌شده** (اگر مهمان است → قبل از پرداخت Login/Register اجباری)
- سبد مهمان تا قبل از پرداخت مجاز است

---

## ۶. معماری صفحات

1. Intro → Home  
2. Shop (ALL / T-SHIRTS / HOODIES / SWEATSHIRTS / JOGGERS)  
3. Product Detail  
4. Collections (editorial)  
5. About 33  
6. Meet Diyar  
7. Search  
8. Account  
9. Cart → Checkout (یک صفحه) → پرداخت  
10. Nav: `۳۳ | SHOP · COLLECTIONS · ABOUT 33 · DIYAR | SEARCH · ACCOUNT · CART`  
11. Mobile: hamburger ، artboard مرجع ۳۹۰ / دسکتاپ ۱۴۴۰  

Admin خارج از ظاهر برند مگر خلافش اعلام شود.

---

## ۷. سیستم بصری

### رنگ
- زمینه: Cream / warm off-white  
- متن: Charcoal / black  
- تأکید: Mustard yellow  
- فرعی: Terracotta ، Sage/Olive ، Warm gray  
- ممنوع: آبی سرد غالب، دارک‌مود گرفته  

> این پالت عمداً از داک برند می‌آید (استثنای قانون کلی «پرهیز از cream+terracotta ژنریک AI»).

### تایپوگرافی پیشنهادی (برای تأیید)

**پشته پیشنهادی A — Editorial مدرن (توصیه اصلی)**  
- نمایشی EN: **Fraunces** یا **Instrument Serif** (تیترهای کمپین)  
- متن/UI EN: **Satoshi** یا **General Sans**  
- فارسی UI + بدنه: **Vazirmatn**  
- فارسی نمایشی (اختیاری تیترهای fa): **Peyda**  

**پشته پیشنهادی B — تمام Sans پریمیوم**  
- EN Display + UI: **Syne** (تیتر) + **Satoshi** (بدنه)  
- FA: **Vazirmatn**  

پیشنهاد قفل: **پشته A** — حس مد editorial را بهتر می‌سازد و با «elegant headlines + clean sans body» داک هم‌خوان است.

### موشن
نرم، آهسته، مطمئن، editorial؛ بدون over-animation.  
Diyar: پرواز Intro، hover جزئی، انتقال‌های انتخابی بین سکشن‌ها.

---

## ۸. قانون ترکیب UI

- Hero = یک ترکیب‌بندی؛ برند/Diyar قوی؛ بدون کارت در Hero  
- محصولات مرکز تجارت؛ Diyar مرکز هویت (نه برعکس)  
- Shop editorial با فضای تنفس؛ hover ظریف + نام/قیمت/Quick Add  
- Product: عکاسی بزرگ + سایز + تعداد + Add + Materials/Size guide/Shipping  
- Diyar هیچ‌وقت محصول را خفه نکند  

---

## ۹. راهنمای ساخت دارایی Diyar (چون آماده نیست)

### چه چیزی لازم است (بسته حداقل فاز ۱)
1. **Diyar Mark** — آیکون تک‌رنگ (favicon / nav)  
2. **Diyar Primary** — پوز اصلی برای Hero/Intro  
3. **۳–۵ پوز** — پرواز، نشسته، نیم‌رخ، نگاه به جلو، روی لباس  
4. **Wordmark ۳۳** مستقل از Diyar  
5. ترجیحاً **SVG** (+ PNG شفاف 2x/3x برای جاهایی که SVG سخت است)

### مشخصات طراحی Diyar
- سیلوئت مینیمال، هندسه تیز، بال/بدن کشیده  
- عدد **۳۳** در negative space بدن خوانا باشد  
- نه کارتونی، نه چاق و بامزه کودکانه  
- در سایز ۱۶px هم قابل‌تشخیص  
- نسخه mono (ذغالی روی cream) و نسخه accent (mustard/terracotta خیلی محدود)

### از کجا تهیه کنیم؟

| روش | مناسب برای | نکته |
|-----|------------|------|
| **استخدام ایلاستریتور برند** (Behance / Instagram / LinkedIn) | بهترین کیفیت | بریف زیر را بفرست؛ خروجی SVG بخواه |
| **Fiverr / Upwork** — “minimal fashion mascot SVG” | سریع و بودجه‌ای | نمونه کار fashion بخواه، نه cartoon |
| **AI draft → اصلاح انسانی** | شروع سریع | با Midjourney/Flux پوز بساز؛ بعد در Illustrator/Figma به SVG تمیز تبدیل شود |
| **طراح ایرانی لوگو/کاراکتر** | هماهنگی با ۳۳ فارسی | برای wordmark ۳۳ و Diyar با هم |

### بریف کوتاه برای طراح/AI
> Minimal fashion brand mascot “Diyar”: elegant tern/seabird silhouette, elongated wings, sharp geometry, sophisticated not cute, number 33 formed by negative space in the body, monochrome charcoal on cream, works as tiny app icon and large editorial illustration, no cartoon eyes, no childish proportions.

### نقش من در فاز بعد (بعد از تأیید پیاده‌سازی)
- لیست پوزها و نام فایل‌ها را نهایی می‌کنم  
- پرامپت‌های دقیق AI می‌دهم  
- چک‌لیست پذیرش SVG (viewBox، mono، خوانایی ۳۳)  
- جای‌گذاری در Intro/Home/Diyar page طبق سیستم طراحی  

تا فایل‌ها آماده شوند می‌توان UI را با **placeholder سیلوئت** پیش برد — ولی ترجیح این است Diyar قبل از polish نهایی برسد.

---

## ۱۰. اتصال Backend

ریپو: `https://github.com/shahab-ahmadpour/Label33-`  
لایه‌ها و سرویس‌های Cart/Checkout/Catalog آماده؛ UI روی همان سوار می‌شود.  
i18n و Intro و ظاهر برند بخش Web هستند.

---

## ۱۱. خارج از قفل فعلی (عمداً بعدی)
- درگاه پرداخت واقعی (فعلاً Mock)  
- Admin با تم برند  
- Wishlist/Review در ناو اصلی فاز ۱ (مگر بخواهی اضافه شود)

---

## ۱۲. چک‌لیست تأیید نهایی قبل از کد

- [x] کانسپت برند و Diyar  
- [x] زبان fa پیش‌فرض + toggle en  
- [x] Intro + رفتار refresh  
- [x] Quick Add مجاز  
- [x] Checkout تک‌صفحه + اجبار Login قبل از پرداخت  
- [x] موافقت با قفل معماری/صفحات  
- [ ] انتخاب فونت: **پشته A** یا **B**؟  
- [ ] مسیر تهیه Diyar: طراح انسانی / AI+اصلاح / ترکیبی؟  
- [ ] تأیید صریح جمله: **«تأیید، برو پیاده‌سازی»**

---

*بدون جمله تأیید پیاده‌سازی، هیچ خط کدی نوشته نمی‌شود.*

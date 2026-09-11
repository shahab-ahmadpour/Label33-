namespace Label33.Web.Localization;

public static class BrandText
{
    private static readonly Dictionary<string, Dictionary<string, string>> Map = new(StringComparer.OrdinalIgnoreCase)
    {
        ["fa"] = new()
        {
            ["nav.shop"] = "فروشگاه",
            ["nav.collections"] = "کالکشن‌ها",
            ["nav.about"] = "درباره 33",
            ["nav.diyar"] = "دیار",
            ["nav.search"] = "جستجو",
            ["nav.account"] = "حساب",
            ["nav.cart"] = "سبد",
            ["nav.login"] = "ورود",
            ["hero.tagline"] = "SAME CLOTHES.\nDIFFERENT MINDS.",
            ["hero.support"] = "33 — نگاهی متفاوت به لباس‌های روزمره.",
            ["hero.cta.shop"] = "مشاهده کالکشن",
            ["hero.cta.diyar"] = "آشنایی با دیار",
            ["shop.title"] = "فروشگاه",
            ["shop.all"] = "همه",
            ["shop.tshirts"] = "تی‌شرت",
            ["shop.hoodies"] = "هودی",
            ["shop.sweatshirts"] = "سویشرت",
            ["shop.joggers"] = "شلوار جاگر",
            ["shop.empty"] = "به‌زودی محصولات جدید می‌آیند.",
            ["shop.quickAdd"] = "افزودن سریع",
            ["product.add"] = "افزودن به سبد",
            ["product.size"] = "سایز",
            ["product.qty"] = "تعداد",
            ["product.materials"] = "جنس",
            ["product.shipping"] = "ارسال",
            ["product.sizeGuide"] = "راهنمای سایز",
            ["collections.title"] = "کالکشن‌ها",
            ["about.title"] = "درباره 33",
            ["about.body"] = "لباس قرار نیست همه را یک شکل کند. 33 درباره ذهن‌های متفاوتی است که یک جهان را جور دیگر می‌بینند.",
            ["diyar.title"] = "آشنایی با دیار",
            ["diyar.intro"] = "دیار نماد بصری 33 است؛ آزاد، کنجکاو، سازگار و متمایز.",
            ["cart.title"] = "سبد خرید",
            ["cart.empty"] = "سبد خالی است.",
            ["cart.checkout"] = "ادامه خرید",
            ["checkout.title"] = "تسویه حساب",
            ["checkout.pay"] = "پرداخت",
            ["checkout.loginRequired"] = "برای پرداخت باید وارد حساب شوید.",
            ["account.login"] = "ورود",
            ["account.register"] = "ثبت‌نام",
            ["account.email"] = "ایمیل",
            ["account.password"] = "رمز عبور",
            ["footer.tag"] = "SAME CLOTHES. DIFFERENT MINDS.",
            ["intro.skip"] = "رد کردن",
            ["common.from"] = "از",
            ["search.placeholder"] = "جستجوی محصول…",
            ["search.resultsFor"] = "نتایج برای",
            ["search.empty"] = "چیزی با این جستجو پیدا نشد."
        },
        ["en"] = new()
        {
            ["nav.shop"] = "Shop",
            ["nav.collections"] = "Collections",
            ["nav.about"] = "About 33",
            ["nav.diyar"] = "Diyar",
            ["nav.search"] = "Search",
            ["nav.account"] = "Account",
            ["nav.cart"] = "Cart",
            ["nav.login"] = "Login",
            ["hero.tagline"] = "SAME CLOTHES.\nDIFFERENT MINDS.",
            ["hero.support"] = "33 — A different perspective on everyday clothing.",
            ["hero.cta.shop"] = "Shop Collection",
            ["hero.cta.diyar"] = "Meet Diyar",
            ["shop.title"] = "Shop",
            ["shop.all"] = "All",
            ["shop.tshirts"] = "T-Shirts",
            ["shop.hoodies"] = "Hoodies",
            ["shop.sweatshirts"] = "Sweatshirts",
            ["shop.joggers"] = "Joggers",
            ["shop.empty"] = "New pieces are on the way.",
            ["shop.quickAdd"] = "Quick Add",
            ["product.add"] = "Add to Cart",
            ["product.size"] = "Size",
            ["product.qty"] = "Quantity",
            ["product.materials"] = "Materials",
            ["product.shipping"] = "Shipping",
            ["product.sizeGuide"] = "Size guide",
            ["collections.title"] = "Collections",
            ["about.title"] = "About 33",
            ["about.body"] = "Clothing is not about everyone looking the same. 33 is about different minds interpreting the same world differently.",
            ["diyar.title"] = "Meet Diyar",
            ["diyar.intro"] = "Diyar is the visual symbol of 33 — free, curious, adaptable, distinctive.",
            ["cart.title"] = "Cart",
            ["cart.empty"] = "Your cart is empty.",
            ["cart.checkout"] = "Checkout",
            ["checkout.title"] = "Checkout",
            ["checkout.pay"] = "Pay",
            ["checkout.loginRequired"] = "Please sign in to complete payment.",
            ["account.login"] = "Login",
            ["account.register"] = "Register",
            ["account.email"] = "Email",
            ["account.password"] = "Password",
            ["footer.tag"] = "SAME CLOTHES. DIFFERENT MINDS.",
            ["intro.skip"] = "Skip",
            ["common.from"] = "From",
            ["search.placeholder"] = "Search products…",
            ["search.resultsFor"] = "Results for",
            ["search.empty"] = "No products matched your search."
        }
    };

    public static string T(string culture, string key)
    {
        culture = culture.StartsWith("en", StringComparison.OrdinalIgnoreCase) ? "en" : "fa";
        if (Map.TryGetValue(culture, out var dict) && dict.TryGetValue(key, out var value))
            return value;
        return key;
    }
}

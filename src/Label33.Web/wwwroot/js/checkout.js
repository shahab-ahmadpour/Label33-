(function () {
  "use strict";

  var form = document.getElementById("checkout-form");
  if (!form) return;

  var phone = document.getElementById("phone");
  var postal = document.getElementById("postalCode");
  var provinceEl = document.getElementById("province");
  var cityEl = document.getElementById("city");
  var lang = (form.getAttribute("data-lang") || "fa").toLowerCase().indexOf("en") === 0 ? "en" : "fa";
  var i18n = {
    selectProvince: form.getAttribute("data-i18n-select-province") || (lang === "en" ? "Select province" : "انتخاب استان"),
    selectCity: form.getAttribute("data-i18n-select-city") || (lang === "en" ? "Select city" : "انتخاب شهر"),
    selectProvinceFirst: form.getAttribute("data-i18n-select-province-first") || (lang === "en" ? "Select a province first" : "ابتدا استان را انتخاب کنید"),
    geoLoadError: form.getAttribute("data-i18n-geo-load-error") || (lang === "en" ? "Could not load provinces" : "خطا در بارگذاری استان‌ها")
  };

  /** @type {{fa:string,en:string,cities:{fa:string,en:string}[]}[]|null} */
  var provinces = null;

  function digitsOnly(value, max) {
    return String(value || "").replace(/\D/g, "").slice(0, max);
  }

  function sanitizePhone(value) {
    var raw = String(value || "");
    var hasPlus = raw.trim().charAt(0) === "+";
    var digits = raw.replace(/\D/g, "").slice(0, 15);
    return hasPlus ? "+" + digits : digits;
  }

  function labelOf(item) {
    return lang === "en" ? item.en : item.fa;
  }

  function findProvince(value) {
    if (!provinces || !value) return null;
    for (var i = 0; i < provinces.length; i++) {
      var p = provinces[i];
      if (p.fa === value || p.en === value) return p;
    }
    return null;
  }

  if (phone) {
    phone.addEventListener("input", function () {
      var next = sanitizePhone(phone.value);
      if (phone.value !== next) phone.value = next;
    });
    phone.addEventListener("paste", function (e) {
      e.preventDefault();
      var text = (e.clipboardData || window.clipboardData).getData("text");
      phone.value = sanitizePhone(text);
    });
  }

  if (postal) {
    postal.addEventListener("input", function () {
      var next = digitsOnly(postal.value, 10);
      if (postal.value !== next) postal.value = next;
    });
    postal.addEventListener("paste", function (e) {
      e.preventDefault();
      var text = (e.clipboardData || window.clipboardData).getData("text");
      postal.value = digitsOnly(text, 10);
    });
  }

  function fillProvinces() {
    if (!provinces || !provinceEl) return;
    var current = provinceEl.value;
    provinceEl.innerHTML = "";
    var placeholder = document.createElement("option");
    placeholder.value = "";
    placeholder.textContent = i18n.selectProvince;
    provinceEl.appendChild(placeholder);

    var sorted = provinces.slice().sort(function (a, b) {
      return labelOf(a).localeCompare(labelOf(b), lang === "en" ? "en" : "fa");
    });

    sorted.forEach(function (p) {
      var opt = document.createElement("option");
      // Canonical Persian value for storage / shipping in Iran.
      opt.value = p.fa;
      opt.textContent = labelOf(p);
      if (p.fa === current || p.en === current) opt.selected = true;
      provinceEl.appendChild(opt);
    });
  }

  function fillCities(provinceValue) {
    if (!cityEl) return;
    cityEl.innerHTML = "";
    var province = findProvince(provinceValue);
    if (!province) {
      cityEl.disabled = true;
      var empty = document.createElement("option");
      empty.value = "";
      empty.textContent = i18n.selectProvinceFirst;
      cityEl.appendChild(empty);
      return;
    }

    cityEl.disabled = false;
    var placeholder = document.createElement("option");
    placeholder.value = "";
    placeholder.textContent = i18n.selectCity;
    cityEl.appendChild(placeholder);

    var cities = province.cities.slice().sort(function (a, b) {
      return labelOf(a).localeCompare(labelOf(b), lang === "en" ? "en" : "fa");
    });

    cities.forEach(function (c) {
      var opt = document.createElement("option");
      opt.value = c.fa;
      opt.textContent = labelOf(c);
      cityEl.appendChild(opt);
    });
  }

  if (provinceEl) {
    provinceEl.addEventListener("change", function () {
      fillCities(provinceEl.value);
    });
  }

  fetch("/data/iran-geo.json")
    .then(function (r) {
      if (!r.ok) throw new Error("geo load failed");
      return r.json();
    })
    .then(function (data) {
      provinces = data.provinces || [];
      fillProvinces();
      if (provinceEl && provinceEl.value) fillCities(provinceEl.value);
    })
    .catch(function () {
      if (provinceEl) {
        provinceEl.innerHTML = "";
        var err = document.createElement("option");
        err.value = "";
        err.textContent = i18n.geoLoadError;
        provinceEl.appendChild(err);
      }
    });

  form.addEventListener("submit", function (e) {
    if (phone) phone.value = sanitizePhone(phone.value);
    if (postal) postal.value = digitsOnly(postal.value, 10);

    var phoneOk = phone && /^\+?[0-9]{1,15}$/.test(phone.value);
    var postalOk = postal && /^[0-9]{1,10}$/.test(postal.value);
    var provinceOk = provinceEl && provinceEl.value;
    var cityOk = cityEl && cityEl.value && !cityEl.disabled;
    var fullNameEl = document.getElementById("fullName");
    var line1El = document.getElementById("line1");
    var fullNameOk = fullNameEl && fullNameEl.value.trim();
    var line1Ok = line1El && line1El.value.trim();

    if (!fullNameOk || !phoneOk || !postalOk || !provinceOk || !cityOk || !line1Ok) {
      e.preventDefault();
      form.reportValidity();
    }
  });
})();

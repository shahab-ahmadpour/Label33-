(function () {
  "use strict";

  var form = document.getElementById("checkout-form");
  if (!form) return;

  var phone = document.getElementById("phone");
  var postal = document.getElementById("postalCode");
  var provinceEl = document.getElementById("province");
  var cityEl = document.getElementById("city");
  /** @type {Record<string, string[]>|null} */
  var geo = null;

  function digitsOnly(value, max) {
    return String(value || "").replace(/\D/g, "").slice(0, max);
  }

  function sanitizePhone(value) {
    var raw = String(value || "");
    var hasPlus = raw.trim().charAt(0) === "+";
    var digits = raw.replace(/\D/g, "").slice(0, 15);
    return hasPlus ? "+" + digits : digits;
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
    if (!geo || !provinceEl) return;
    var current = provinceEl.value;
    provinceEl.innerHTML = '<option value="">انتخاب استان</option>';
    Object.keys(geo)
      .sort(function (a, b) {
        return a.localeCompare(b, "fa");
      })
      .forEach(function (name) {
        var opt = document.createElement("option");
        opt.value = name;
        opt.textContent = name;
        if (name === current) opt.selected = true;
        provinceEl.appendChild(opt);
      });
  }

  function fillCities(provinceName) {
    if (!cityEl) return;
    cityEl.innerHTML = "";
    if (!provinceName || !geo || !geo[provinceName]) {
      cityEl.disabled = true;
      var empty = document.createElement("option");
      empty.value = "";
      empty.textContent = "ابتدا استان را انتخاب کنید";
      cityEl.appendChild(empty);
      return;
    }

    cityEl.disabled = false;
    var placeholder = document.createElement("option");
    placeholder.value = "";
    placeholder.textContent = "انتخاب شهر";
    cityEl.appendChild(placeholder);

    geo[provinceName].forEach(function (name) {
      var opt = document.createElement("option");
      opt.value = name;
      opt.textContent = name;
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
      geo = data;
      fillProvinces();
      if (provinceEl && provinceEl.value) fillCities(provinceEl.value);
    })
    .catch(function () {
      if (provinceEl) {
        provinceEl.innerHTML = '<option value="">خطا در بارگذاری استان‌ها</option>';
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

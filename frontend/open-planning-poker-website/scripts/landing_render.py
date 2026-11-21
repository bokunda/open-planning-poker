"""Shared page shell for scripts/make-landing-pages.py (OPP-176): the same head, header, footer and call to action as the
guides, plus optional FAQ and HowTo structured data, a picture and a small inline tool."""
import html
import json

SITE = "https://openplanningpoker.com"
APP = "https://app.openplanningpoker.com"
LANG_CODE = {"en": "en", "sr": "sr-Latn", "de": "de"}
OG_LOCALE = {"en": None, "sr": "sr_RS", "de": "de_DE"}

UI = {
    "en": {"home": "Home", "more": "Related pages", "faq": "Questions", "updated": "Last updated",
           "ctaTitle": "Try it with your team.", "ctaText": "Free, no sign-up. Share one link and you are playing.", "cta": "Start a game"},
    "sr": {"home": "Početna", "more": "Povezane stranice", "faq": "Pitanja", "updated": "Poslednja izmena",
           "ctaTitle": "Probaj sa svojim timom.", "ctaText": "Besplatno, bez registracije. Podeli jedan link i igra počinje.", "cta": "Pokreni igru"},
    "de": {"home": "Startseite", "more": "Verwandte Seiten", "faq": "Fragen", "updated": "Zuletzt geändert",
           "ctaTitle": "Probier es mit deinem Team aus.", "ctaText": "Kostenlos, ohne Anmeldung. Teile einen Link und ihr spielt.", "cta": "Spiel starten"},
}

IMAGES = {
    "room": ("app-room-light", 1600, 1000),
    "voting": ("app-voting-light", 1600, 1000),
    "new-game": ("app-new-game", 1600, 1000),
}


def prefix(lang):
    return "/" if lang == "en" else f"/{lang}/"


def picture(name, alt):
    base, w, h = IMAGES[name]
    srcset = ", ".join(f"/images/{base}-{size}.webp {size}w" for size in (480, 800, 1200))
    return (f'<figure class="not-prose my-8"><img src="/images/{base}-800.webp" srcset="{srcset}" '
            f'sizes="(min-width: 768px) 720px, 100vw" alt="{html.escape(alt)}" width="{w}" height="{h}" loading="lazy" decoding="async" '
            f'class="w-full h-auto rounded-xl border border-gray-200 dark:border-gray-800 shadow-sm"></figure>')


def render(slug, lang, p, related, date, kind="WebPage"):
    """p: title, nav, description, lead, body; optional faq [(q, a)], howto {name, steps}, app (SoftwareApplication)."""
    ui = UI[lang]
    url = f"{SITE}{prefix(lang)}{slug}/"
    esc = html.escape
    graph = [
        {"@type": "BreadcrumbList", "itemListElement": [
            {"@type": "ListItem", "position": 1, "name": ui["home"], "item": f"{SITE}{prefix(lang)}"},
            {"@type": "ListItem", "position": 2, "name": p["nav"], "item": url},
        ]},
        {"@type": kind, "name": p["title"], "headline": p["title"], "description": p["description"], "url": url,
         "dateModified": date, "inLanguage": LANG_CODE[lang], "publisher": {"@id": f"{SITE}/#org"}},
    ]
    if kind == "Article":
        graph[1]["datePublished"] = date
        graph[1]["image"] = f"{SITE}/images/og-card.png"
        graph[1]["author"] = {"@type": "Organization", "name": "Open Planning Poker", "url": SITE}
        del graph[1]["name"]
    else:
        del graph[1]["headline"]
    if p.get("faq"):
        graph.append({"@type": "FAQPage", "mainEntity": [
            {"@type": "Question", "name": q, "acceptedAnswer": {"@type": "Answer", "text": a}} for q, a in p["faq"]]})
    if p.get("howto"):
        h = p["howto"]
        graph.append({"@type": "HowTo", "name": h["name"], "inLanguage": LANG_CODE[lang], "totalTime": h.get("time", "PT10M"),
                      "step": [{"@type": "HowToStep", "position": i + 1, "name": n, "text": t} for i, (n, t) in enumerate(h["steps"])]})
    if p.get("app"):
        graph.append({"@type": "SoftwareApplication", "name": p["app"], "applicationCategory": "BusinessApplication",
                      "operatingSystem": "Web browser", "url": url, "isAccessibleForFree": True,
                      "offers": {"@type": "Offer", "price": "0", "priceCurrency": "EUR"}, "publisher": {"@id": f"{SITE}/#org"}})
    ld = {"@context": "https://schema.org", "@graph": graph}
    og_locale = f'\n    <meta property="og:locale" content="{OG_LOCALE[lang]}">' if OG_LOCALE[lang] else ""

    faq_html = ""
    if p.get("faq"):
        items = "\n".join(f"<h3>{esc(q)}</h3>\n<p>{esc(a)}</p>" for q, a in p["faq"])
        faq_html = f'\n<h2 id="faq">{esc(ui["faq"])}</h2>\n{items}'

    others = "\n".join(f'          <li><a href="{prefix(lang)}{other}/" class="link">{esc(name)}</a></li>' for other, name in related)
    tool = p.get("tool", "")
    script = f"\n    <script>\n{p['script'].strip()}\n    </script>" if p.get("script") else ""
    cta_href = p.get("cta_href", APP)
    width = "max-w-4xl" if p.get("wide") else "max-w-3xl"
    return f"""<!DOCTYPE html>
<html lang="{LANG_CODE[lang]}">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>{esc(p['title'])} | Open Planning Poker</title>
    <meta name="description" content="{esc(p['description'])}">
    <meta name="robots" content="index, follow, max-image-preview:large, max-snippet:-1">
    <link rel="canonical" href="{url}">
    <meta property="og:type" content="{'article' if kind == 'Article' else 'website'}">
    <meta property="og:url" content="{url}">{og_locale}
    <meta property="og:title" content="{esc(p['title'])}">
    <meta property="og:description" content="{esc(p['description'])}">
    <meta property="og:image" content="{SITE}/images/og-card.png">
    <meta property="og:image:width" content="1200">
    <meta property="og:image:height" content="630">
    <meta property="og:site_name" content="Open Planning Poker">
    <meta name="twitter:card" content="summary_large_image">
    <meta name="twitter:title" content="{esc(p['title'])}">
    <meta name="twitter:description" content="{esc(p['description'])}">
    <meta name="twitter:image" content="{SITE}/images/og-card.png">
    <!-- SITE_HEAD -->
    <script type="application/ld+json">
{json.dumps(ld, ensure_ascii=False, indent=2)}
    </script>
</head>
<body class="bg-white text-gray-600 font-sans antialiased dark:bg-gray-900 dark:text-gray-400">
    <!-- SITE_HEADER -->

    <main id="main">
      <article class="{width} px-4 pt-28 pb-16 mx-auto lg:pt-36">
        <nav aria-label="Breadcrumb" class="text-sm">
          <ol class="flex gap-2">
            <li><a href="{prefix(lang)}" class="link">{esc(ui['home'])}</a></li>
            <li aria-hidden="true">/</li>
            <li aria-current="page">{esc(p['nav'])}</li>
          </ol>
        </nav>
        <h1 class="mt-6 font-display text-4xl font-extrabold tracking-tight text-gray-900 sm:text-5xl dark:text-white">{esc(p['title'])}</h1>
        <p class="mt-5 text-lg leading-relaxed">{esc(p['lead'])}</p>
{tool}
        <div class="guide mt-10">
{p['body'].strip()}{faq_html}
<p class="mt-10 text-sm">{esc(ui['updated'])}: <time datetime="{date}">{date}</time></p>
        </div>

        <aside class="mt-12 p-6 border border-gray-200 rounded-2xl dark:border-gray-800 bg-gray-50 dark:bg-gray-800/50" aria-labelledby="more-pages">
          <h2 id="more-pages" class="font-display text-lg font-extrabold text-gray-900 dark:text-white">{esc(ui['more'])}</h2>
          <ul class="mt-3 space-y-2">
{others}
          </ul>
        </aside>
      </article>

      <section class="px-4 pb-16 lg:pb-24">
        <div class="cta-band max-w-(--breakpoint-lg) mx-auto">
          <h2 class="font-display text-3xl font-extrabold tracking-tight text-white sm:text-4xl">{esc(p.get('ctaTitle', ui['ctaTitle']))}</h2>
          <p class="mt-3 text-lg text-purple-100">{esc(p.get('ctaText', ui['ctaText']))}</p>
          <a href="{cta_href}" class="inline-flex items-center justify-center mt-8 px-6 py-3.5 font-bold rounded-full bg-white text-purple-700 shadow-lg hover:bg-purple-50 focus:outline-hidden focus-visible:ring-4 focus-visible:ring-white/60 transition">{esc(p.get('cta', ui['cta']))}</a>
        </div>
      </section>
    </main>

    <!-- SITE_FOOTER -->{script}
</body>
</html>
"""

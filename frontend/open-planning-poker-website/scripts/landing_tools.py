"""The two free tools of the website (OPP-177, OPP-178): the markup per language and one script each. They run in the
browser only; nothing is sent anywhere."""
import html
import json

INPUT = ("w-full rounded-lg border border-gray-300 bg-white px-3 py-2 text-gray-900 focus:border-purple-600 "
         "focus:outline-hidden focus-visible:ring-2 focus-visible:ring-purple-600/40 dark:border-gray-700 dark:bg-gray-900 dark:text-white")
BUTTON = ("inline-flex items-center justify-center px-4 py-2 font-semibold rounded-full border border-gray-300 text-gray-800 "
          "hover:bg-gray-50 focus-ring dark:border-gray-700 dark:text-gray-100 dark:hover:bg-gray-800")
PRIMARY = ("inline-flex items-center justify-center px-5 py-2.5 font-bold rounded-full bg-purple-700 text-white shadow "
           "hover:bg-purple-800 focus-ring")
BOX = "mt-10 p-5 sm:p-6 border border-gray-200 rounded-2xl bg-gray-50 dark:border-gray-800 dark:bg-gray-800/50"

# ---------------------------------------------------------------------------------------------------- velocity

VELOCITY_TEXT = {
    "en": {
        "title": "Your last sprints", "sprint": "Sprint", "points": "Points done", "personDays": "Person-days (optional)",
        "addSprint": "Add a sprint", "next": "The next sprint", "length": "Sprint length (working days)",
        "member": "Team member", "days": "Days available", "addMember": "Add a member", "remove": "Remove",
        "result": "Suggested for the next sprint", "points_unit": "points", "range": "Range from your sprints: {low}–{high} points.",
        "byCapacity": "Based on {rate} points per person-day of {count} sprints and {days} person-days now.",
        "byAverage": "Based on the average of your last {count} sprints. Add person-days to plan with this sprint's capacity.",
        "needMore": "Enter the points of at least two sprints.",
        "memberName": "Member {n}",
    },
    "sr": {
        "title": "Tvoji poslednji sprintovi", "sprint": "Sprint", "points": "Urađeno poena", "personDays": "Čovek-dani (opciono)",
        "addSprint": "Dodaj sprint", "next": "Sledeći sprint", "length": "Dužina sprinta (radni dani)",
        "member": "Član tima", "days": "Dostupni dani", "addMember": "Dodaj člana", "remove": "Ukloni",
        "result": "Predlog za sledeći sprint", "points_unit": "poena", "range": "Raspon prema tvojim sprintovima: {low}–{high} poena.",
        "byCapacity": "Na osnovu {rate} poena po čovek-danu iz {count} sprintova i {days} čovek-dana sada.",
        "byAverage": "Na osnovu proseka poslednjih {count} sprintova. Dodaj čovek-dane da planiraš prema kapacitetu ovog sprinta.",
        "needMore": "Unesi poene za bar dva sprinta.",
        "memberName": "Član {n}",
    },
    "de": {
        "title": "Deine letzten Sprints", "sprint": "Sprint", "points": "Erledigte Punkte", "personDays": "Personentage (optional)",
        "addSprint": "Sprint hinzufügen", "next": "Der nächste Sprint", "length": "Sprint-Länge (Arbeitstage)",
        "member": "Teammitglied", "days": "Verfügbare Tage", "addMember": "Mitglied hinzufügen", "remove": "Entfernen",
        "result": "Vorschlag für den nächsten Sprint", "points_unit": "Punkte", "range": "Spanne aus deinen Sprints: {low}–{high} Punkte.",
        "byCapacity": "Auf Basis von {rate} Punkten pro Personentag aus {count} Sprints und {days} Personentagen jetzt.",
        "byAverage": "Auf Basis des Durchschnitts deiner letzten {count} Sprints. Trag Personentage ein, um mit der Kapazität dieses Sprints zu planen.",
        "needMore": "Gib die Punkte von mindestens zwei Sprints ein.",
        "memberName": "Mitglied {n}",
    },
}


def velocity_tool(lang):
    t = VELOCITY_TEXT[lang]
    e = html.escape
    return f"""
        <section class="{BOX}" aria-labelledby="calc-title" id="velocity-calculator" data-text='{e(json.dumps(t, ensure_ascii=False))}'>
          <h2 id="calc-title" class="font-display text-xl font-extrabold text-gray-900 dark:text-white">{e(t['title'])}</h2>
          <div class="mt-4 overflow-x-auto">
            <table class="w-full text-sm">
              <thead><tr class="text-left"><th class="py-1 pr-2">{e(t['sprint'])}</th><th class="py-1 pr-2">{e(t['points'])}</th><th class="py-1 pr-2">{e(t['personDays'])}</th><th class="py-1"><span class="sr-only">{e(t['remove'])}</span></th></tr></thead>
              <tbody data-rows="sprints"></tbody>
            </table>
          </div>
          <button type="button" class="{BUTTON} mt-3" data-add="sprints">+ {e(t['addSprint'])}</button>

          <h2 class="mt-8 font-display text-xl font-extrabold text-gray-900 dark:text-white">{e(t['next'])}</h2>
          <label class="mt-4 block text-sm font-semibold text-gray-800 dark:text-gray-200" for="sprint-length">{e(t['length'])}</label>
          <input id="sprint-length" type="number" min="1" max="30" value="10" inputmode="numeric" class="{INPUT} mt-1 max-w-40">
          <div class="mt-4 overflow-x-auto">
            <table class="w-full text-sm">
              <thead><tr class="text-left"><th class="py-1 pr-2">{e(t['member'])}</th><th class="py-1 pr-2">{e(t['days'])}</th><th class="py-1"><span class="sr-only">{e(t['remove'])}</span></th></tr></thead>
              <tbody data-rows="members"></tbody>
            </table>
          </div>
          <button type="button" class="{BUTTON} mt-3" data-add="members">+ {e(t['addMember'])}</button>

          <div class="mt-8 p-5 rounded-xl bg-white dark:bg-gray-900 border border-purple-200 dark:border-purple-900" aria-live="polite">
            <p class="text-sm font-semibold text-gray-800 dark:text-gray-200">{e(t['result'])}</p>
            <p class="mt-1 font-display text-4xl font-extrabold text-purple-700 dark:text-purple-300" data-out="suggested">–</p>
            <p class="mt-2 text-sm" data-out="range"></p>
            <p class="mt-1 text-sm" data-out="method"></p>
          </div>
        </section>
"""


VELOCITY_SCRIPT = r"""
(function () {
  var root = document.getElementById('velocity-calculator');
  if (!root) return;
  var t = JSON.parse(root.dataset.text);
  var inputClass = '__INPUT__';
  var fmt = function (s, v) { return s.replace(/\{(\w+)\}/g, function (_, k) { return v[k]; }); };
  var num = function (el) { var n = parseFloat(String(el.value).replace(',', '.')); return isFinite(n) && n >= 0 ? n : null; };

  function cell(type, value, label) {
    var td = document.createElement('td'); td.className = 'py-1 pr-2';
    var input = document.createElement('input');
    input.type = type; input.value = value; input.className = inputClass; input.setAttribute('aria-label', label);
    if (type === 'number') { input.min = '0'; input.step = '0.5'; input.inputMode = 'decimal'; }
    input.addEventListener('input', compute);
    td.appendChild(input); return td;
  }
  function removeCell(tr) {
    var td = document.createElement('td'); td.className = 'py-1';
    var b = document.createElement('button'); b.type = 'button'; b.textContent = '×'; b.className = 'px-2 text-lg leading-none focus-ring rounded-sm';
    b.setAttribute('aria-label', t.remove);
    b.addEventListener('click', function () { tr.remove(); compute(); });
    td.appendChild(b); return td;
  }
  function addSprint(points, days) {
    var body = root.querySelector('[data-rows="sprints"]'); var n = body.children.length + 1;
    var tr = document.createElement('tr');
    var name = document.createElement('td'); name.className = 'py-1 pr-2 whitespace-nowrap'; name.textContent = t.sprint + ' ' + n;
    tr.append(name, cell('number', points, t.points + ' ' + n), cell('number', days, t.personDays + ' ' + n), removeCell(tr));
    body.appendChild(tr);
  }
  function addMember(days) {
    var body = root.querySelector('[data-rows="members"]'); var n = body.children.length + 1;
    var tr = document.createElement('tr');
    tr.append(cell('text', fmt(t.memberName, { n: n }), t.member + ' ' + n), cell('number', days, t.days + ' ' + n), removeCell(tr));
    body.appendChild(tr);
  }
  function compute() {
    var length = num(document.getElementById('sprint-length')) || 10;
    var past = Array.prototype.map.call(root.querySelectorAll('[data-rows="sprints"] tr'), function (tr) {
      var inputs = tr.querySelectorAll('input'); return { points: num(inputs[0]), days: num(inputs[1]) };
    }).filter(function (s) { return s.points !== null; }).reverse();
    var personDays = Array.prototype.reduce.call(root.querySelectorAll('[data-rows="members"] tr'), function (sum, tr) {
      var d = num(tr.querySelectorAll('input')[1]); return sum + Math.min(length, d === null ? length : d);
    }, 0);
    var out = function (k) { return root.querySelector('[data-out="' + k + '"]'); };
    // The same rule as the team plan in the app: points per person-day when at least two sprints have it,
    // otherwise the average of the last three sprints.
    var withDays = past.filter(function (s) { return s.days > 0; }).slice(0, 3);
    var suggested, low, high, method;
    if (withDays.length >= 2 && personDays > 0) {
      var rate = withDays.reduce(function (a, s) { return a + s.points; }, 0) / withDays.reduce(function (a, s) { return a + s.days; }, 0);
      var rates = withDays.map(function (s) { return s.points / s.days; });
      suggested = Math.floor(rate * personDays);
      low = Math.floor(Math.min.apply(null, rates) * personDays); high = Math.floor(Math.max.apply(null, rates) * personDays);
      method = fmt(t.byCapacity, { rate: rate.toFixed(2), count: withDays.length, days: personDays });
    } else {
      var recent = past.slice(0, 3);
      if (recent.length < 2) { out('suggested').textContent = '–'; out('range').textContent = ''; out('method').textContent = t.needMore; return; }
      var pts = recent.map(function (s) { return s.points; });
      suggested = Math.floor(pts.reduce(function (a, b) { return a + b; }, 0) / pts.length);
      low = Math.floor(Math.min.apply(null, pts)); high = Math.floor(Math.max.apply(null, pts));
      method = fmt(t.byAverage, { count: recent.length });
    }
    out('suggested').textContent = suggested + ' ' + t.points_unit;
    out('range').textContent = fmt(t.range, { low: low, high: high });
    out('method').textContent = method;
  }
  root.querySelector('[data-add="sprints"]').addEventListener('click', function () { addSprint('', ''); compute(); });
  root.querySelector('[data-add="members"]').addEventListener('click', function () { addMember(''); compute(); });
  document.getElementById('sprint-length').addEventListener('input', compute);
  [[21, 38], [26, 40], [24, 36]].forEach(function (s) { addSprint(s[0], s[1]); });
  [10, 10, 8, 10].forEach(function (d) { addMember(d); });
  compute();
})();
""".replace("__INPUT__", INPUT)

# ---------------------------------------------------------------------------------------------------- scales

SCALES_TEXT = {
    "en": {
        "title": "The scales side by side", "size": "Size", "example": "Example ticket", "use": "Use this deck",
        "converter": "T-shirt sizes to points", "converterLead": "Agree on a mapping with your team, then copy it into your team wiki.",
        "copy": "Copy as a table", "copied": "Copied.",
        "decks": ["Fibonacci", "Modified Fibonacci", "Powers of 2", "T-shirt sizes"],
        "examples": ["Fix a typo on a page", "Add a field to a form", "A new filter on a list", "A small feature with tests",
                     "A feature across front end and back end", "A new integration with another system", "Too big: split it"],
    },
    "sr": {
        "title": "Skale jedna pored druge", "size": "Veličina", "example": "Primer tiketa", "use": "Koristi ovaj špil",
        "converter": "Veličine majica u poene", "converterLead": "Dogovori preslikavanje sa timom, pa ga kopiraj u timski wiki.",
        "copy": "Kopiraj kao tabelu", "copied": "Kopirano.",
        "decks": ["Fibonacci", "Modifikovani Fibonacci", "Stepeni dvojke", "Veličine majica"],
        "examples": ["Ispravka greške u kucanju na stranici", "Novo polje u formi", "Novi filter na listi", "Mala funkcija sa testovima",
                     "Funkcija i na front endu i na back endu", "Nova integracija sa drugim sistemom", "Preveliko: podeli ga"],
    },
    "de": {
        "title": "Die Skalen nebeneinander", "size": "Größe", "example": "Beispiel-Ticket", "use": "Dieses Deck nutzen",
        "converter": "T-Shirt-Größen in Punkte", "converterLead": "Einigt euch im Team auf eine Zuordnung und kopiert sie ins Team-Wiki.",
        "copy": "Als Tabelle kopieren", "copied": "Kopiert.",
        "decks": ["Fibonacci", "Modifiziertes Fibonacci", "Zweierpotenzen", "T-Shirt-Größen"],
        "examples": ["Tippfehler auf einer Seite beheben", "Ein Feld in einem Formular ergänzen", "Ein neuer Filter in einer Liste", "Eine kleine Funktion mit Tests",
                     "Eine Funktion in Frontend und Backend", "Eine neue Anbindung an ein anderes System", "Zu groß: aufteilen"],
    },
}

# Rows from smallest to largest: each deck's card for that size.
SCALE_ROWS_FULL = [
    ("½", "½", "1", "XS"), ("1", "1", "2", "S"), ("2", "2", "4", "S"), ("3", "3", "4", "M"),
    ("5", "5", "8", "M"), ("8", "8", "16", "L"), ("13", "20", "32", "XL"),
]
DECK_NAMES = ["fibonacci", "fibonacci2", "powers", "tshirt"]
TSHIRT_DEFAULTS = [("XS", 1), ("S", 2), ("M", 3), ("L", 5), ("XL", 8), ("XXL", 13)]


def scales_tool(lang, app):
    t = SCALES_TEXT[lang]
    e = html.escape
    head = "".join(f'<th class="py-2 pr-3">{e(name)}</th>' for name in t["decks"])
    rows = "\n".join(
        "<tr class=\"border-t border-gray-200 dark:border-gray-700\">" + "".join(f'<td class="py-2 pr-3 font-semibold text-gray-900 dark:text-white">{v}</td>' for v in r)
        + f'<td class="py-2">{e(t["examples"][i])}</td></tr>' for i, r in enumerate(SCALE_ROWS_FULL))
    rows += f'\n<tr class="border-t border-gray-200 dark:border-gray-700"><td class="py-2 pr-3 font-semibold">40</td><td class="py-2 pr-3 font-semibold">40, 100</td><td class="py-2 pr-3 font-semibold">64</td><td class="py-2 pr-3 font-semibold">XXL</td><td class="py-2">{e(t["examples"][6])}</td></tr>'
    use = "".join(f'<td class="py-2 pr-3"><a class="link text-sm" href="{app}/?deck={d}">{e(t["use"])}</a></td>' for d in DECK_NAMES)
    conv = "\n".join(
        f'<tr><td class="py-1 pr-3 font-semibold text-gray-900 dark:text-white">{s}</td><td class="py-1"><input type="number" min="0" step="0.5" value="{v}" '
        f'aria-label="{e(t["size"])} {s}" data-size="{s}" class="{INPUT} max-w-28"></td></tr>' for s, v in TSHIRT_DEFAULTS)
    return f"""
        <section class="{BOX}" aria-labelledby="scales-title">
          <h2 id="scales-title" class="font-display text-xl font-extrabold text-gray-900 dark:text-white">{e(t['title'])}</h2>
          <div class="mt-4 overflow-x-auto">
            <table class="w-full text-sm text-left">
              <thead><tr>{head}<th class="py-2">{e(t['example'])}</th></tr></thead>
              <tbody>
{rows}
              </tbody>
              <tfoot><tr class="border-t border-gray-200 dark:border-gray-700">{use}<td></td></tr></tfoot>
            </table>
          </div>
        </section>

        <section class="{BOX}" aria-labelledby="converter-title" id="converter" data-copied="{e(t['copied'])}" data-size-label="{e(t['size'])}" data-points-label="{e(VELOCITY_TEXT[lang]['points_unit'])}">
          <h2 id="converter-title" class="font-display text-xl font-extrabold text-gray-900 dark:text-white">{e(t['converter'])}</h2>
          <p class="mt-2 text-sm">{e(t['converterLead'])}</p>
          <table class="mt-4 text-sm"><tbody>
{conv}
          </tbody></table>
          <button type="button" class="{PRIMARY} mt-4" data-copy>{e(t['copy'])}</button>
          <span class="ml-3 text-sm" aria-live="polite" data-status></span>
        </section>
"""


SCALES_SCRIPT = r"""
(function () {
  var root = document.getElementById('converter');
  if (!root) return;
  root.querySelector('[data-copy]').addEventListener('click', function () {
    var rows = Array.prototype.map.call(root.querySelectorAll('input[data-size]'), function (i) { return i.dataset.size + '\t' + i.value; });
    var text = root.dataset.sizeLabel + '\t' + root.dataset.pointsLabel + '\n' + rows.join('\n');
    var done = function () { root.querySelector('[data-status]').textContent = root.dataset.copied; };
    if (navigator.clipboard) navigator.clipboard.writeText(text).then(done); else done();
  });
})();
"""

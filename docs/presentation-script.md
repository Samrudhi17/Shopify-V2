# ScanStore — Presentation Script

**Live demo:** http://46.225.136.102
**Total time:** ~25 minutes speaking + 5 minutes Q&A
**Presenters:** 5 × ~5 minutes each

> Square brackets are stage directions, not words to read out.
> `[SLIDE]` = change slide · `[DEMO]` = switch to the browser · `[PAUSE]` = let it land.

**Before you start:** open the site, log in as vendor and as admin in two tabs,
and have the catalog page ready. Recording a 2-minute backup video of the full
flow is strongly advised — projector Wi-Fi fails more often than you'd think.

---

## Phase 1 — Problem & Objectives
**Presenter 1 · 5 minutes**

[SLIDE: title]

> Good morning. We're presenting **ScanStore** — a QR-based digital shop
> catalog platform. I'm going to start with the problem we set out to solve.

[SLIDE: problem]

> Walk into any cloth shop in Kothrud or Laxmi Road. The owner has stock worth
> lakhs, but if you ask "what sarees do you have in red, size medium?" the only
> answer is to physically show you. There's no way for a customer to browse
> before walking in, and no way for the shop to reach anyone who isn't already
> standing in the doorway.
>
> These shops aren't online for three reasons. A website costs money they can't
> justify. Platforms like Shopify start around ₹2,000 a month and are built for
> sellers who ship nationwide. And WhatsApp or Instagram, which is what most of
> them actually use, has no structure — no categories, no stock, no search. It's
> a photo album, not a catalog.

[PAUSE]

> So the problem is: **local retailers need an online catalog, not an online
> store.** They don't want to ship. They don't want payments. They want a
> customer to see what's in stock before visiting.

[SLIDE: objectives]

> Our objectives were:
>
> One — let a shop owner register and publish a live catalog in under ten
> minutes, with no technical knowledge.
>
> Two — give every shop a **unique URL and a QR code** they can print and stick
> on the counter, so a customer scans and browses instantly, with no app to
> install.
>
> Three — give them real inventory management: categories, product variants by
> colour and size, and stock that updates.
>
> Four — make it sustainable as a product, with a subscription model priced for
> this market.

[SLIDE: three users]

> The system has three kinds of user. **Vendors** are shop owners who manage
> their catalog. **Admins** oversee the platform and can take a shop offline.
> And **customers**, who never log in at all — they just scan and browse.
>
> My colleague will now take you through how we built it.

### Q&A you should be ready for
- *"Why not just use WhatsApp Business?"* → It has no categories, no stock levels, no search, and no single link to share. We're structured data, not a photo feed.
- *"Who is your actual customer?"* → Single-outlet clothing retailers in tier-2 cities. We scoped to cloth shops so the category model — colour, size, variants — could be concrete rather than generic.
- *"Is this an e-commerce site?"* → Deliberately not. No cart, no payments, no shipping. Adding those triples the complexity and none of our target shops asked for it.

---

## Phase 2 — System Design & Architecture
**Presenter 2 · 5 minutes**

[SLIDE: architecture diagram]

> I'll cover how the system is put together.
>
> It's a three-tier application. A **React front end** built with Vite. A
> **.NET 10 Web API** that holds all the business logic. And **MySQL 8** for
> storage, accessed through Entity Framework Core. Everything runs in Docker.

[SLIDE: request flow]

> A request flows like this. The browser talks to **nginx**, which serves the
> React bundle and proxies anything under `/api/` to the .NET container. The API
> talks to MySQL on a private Docker network — the database isn't reachable from
> the internet at all. Only port 80 is exposed.

[SLIDE: authentication]

> For authentication we use **Firebase**. The client signs in with Firebase and
> gets an ID token; every API request carries that token as a bearer. The API
> verifies it against Google's public signing keys.
>
> The reason that matters: **we never store passwords.** There's no password
> column in our database, so we can't leak one. Password resets, and later
> things like Google sign-in, come free.

[SLIDE: ER diagram]

> The data model has 14 entities. The important design decision is here —

[Point at Products → Product_Variants → Inventory]

> A product isn't one row. A saree is a **Product**; each colour-and-size
> combination is a **Product_Variant**; and each variant has its own
> **Inventory** row. That's three tables where a naive design would use one.
>
> We did that because a shop doesn't have "twelve sarees" — it has four red in
> medium and two blue in large. If stock lived on the product you could never
> answer "is the red one available?" And every stock change writes a row to
> **Stock_History**, so the shop has an audit trail, not just a current number.

[SLIDE: QR generation]

> The QR flow: when a shop registers, we slugify its name — "Gokul Cloth Store"
> becomes `gokul-cloth-store` — check it's unique, build the catalog URL from it,
> and render a QR PNG encoding that URL. Print it, stick it on the counter, done.
>
> Next, the vendor experience.

### Q&A you should be ready for
- *"Why MySQL and not SQL Server?"* → We started on SQL Server. Microsoft publishes no ARM64 image, which blocked deployment to ARM cloud servers. We migrated to MySQL — it has native ARM images and no database size cap. That migration is in our commit history.
- *"Why Firebase instead of your own auth?"* → Rolling your own auth means storing password hashes, handling resets, and rate-limiting brute force. Firebase does all of that, and it means a breach of our database exposes no credentials.
- *"How do you stop one vendor reading another's data?"* → Every protected endpoint resolves the vendor from the **token**, not from an ID in the request body. Asking for someone else's shop returns 404.

---

## Phase 3 — Vendor Module *(live demo)*
**Presenter 3 · 5 minutes**

[DEMO: registration page]

> This is what a shop owner sees. I'll register as a new vendor.

[Type `Samrudhi` in the Name field, then click away]

> Notice the error appears **under that specific field**, as I type — not after
> submitting. It's asking for a full name, first and last.

[Type `111111111111` in Aadhaar]

> Aadhaar rejects this. Twelve digits isn't enough — a real Aadhaar never starts
> with 0 or 1, and it's never the same digit repeated. Same for phone:

[Type `9999999999`]

> Ten digits, but it's rejected — Indian mobiles start 6 to 9 and this is
> obviously fake.

[PAUSE]

> Every one of these rules exists in **two places**: in the browser for instant
> feedback, and again in the API. The client-side checks are a convenience. The
> server-side checks are the actual guard, because anyone can bypass a browser.

[DEMO: shop registration]

> Once registered, the vendor sets up their shop. Watch the shop name field —

[Type an existing shop name, e.g. `Gokul`]

> It checks against the database as I type and tells me it's taken. That's a
> live lookup, debounced so it isn't hammering the server on every keystroke.

[Complete the form and submit]

> And immediately the shop has a **catalog URL and a QR code**, generated
> automatically. That's the core promise — ten minutes from signup to a printable
> QR code.

[DEMO: add product with multiple images]

> Adding a product. Name, category, type, colour, size, price, stock. I can add
> **up to six images** — and they accumulate, so I can pick a few and add more.
> The first one is the cover; I can promote any image to cover.

[Click the AI generate button if present]

> We also added AI-assisted descriptions — one call to Google's Gemini drafts a
> product description, so a shopkeeper who doesn't want to write copy doesn't
> have to.

> Now the admin side and what the customer sees.

### Q&A you should be ready for
- *"Why validate twice — isn't that duplicated code?"* → The rules live in **one file per side** and are deliberately mirrored. Client-only validation is bypassable with devtools; server-only means users don't see an error until submit. You need both.
- *"What if two vendors pick the same shop name at once?"* → The live check is advisory. The create endpoint re-checks and returns a conflict, because someone can claim the name in the gap between the check and the submit.
- *"How big can uploads be?"* → Certificates 3 MB, logos and product images 500 KB, enforced in the browser and again in the API, with per-folder file-type rules.

---

## Phase 4 — Admin Console & Customer Experience *(live demo)*
**Presenter 4 · 5 minutes**

[DEMO: admin dashboard]

> This is the admin console. Total vendors, total shops, and how many are active
> or inactive, with the shop list below.

[DEMO: admin → Vendors]

> Here's a design decision worth explaining. The vendor list is **read-only** —
> an admin cannot deactivate a vendor.

[DEMO: admin → Shops, click Deactivate]

> Instead they deactivate the **shop**. And watch what that does —

[DEMO: open the public catalog for that shop]

> The catalog is gone. Deactivating a shop takes it off the public directory and
> its catalog URL stops serving.
>
> We separated these deliberately. Suspending a *business* and deleting a
> *person's account* are different actions with different consequences. The
> admin controls the shop's visibility; the vendor's account and data stay
> intact. We also removed the vendor-status endpoint entirely, so it isn't just a
> hidden button — the capability doesn't exist in the API.

[Reactivate the shop]

[DEMO: public site → Shops]

> Now the customer side. This is the public directory — every active shop,
> searchable, no login.

[DEMO: open a shop catalog]

> And this is what a customer sees after scanning. Shop name, contact, location,
> and the products. Search by name or brand, filter by category, type, size and
> colour.

[Swipe the image carousel on a multi-image product]

> Products with several photos show them in a **swipeable gallery**. On a phone
> that's a real finger swipe — we used CSS scroll-snap rather than a carousel
> library, so there's no extra JavaScript to download.

[DEMO: hold up a phone and scan the QR, or show the QR page]

> And this is the whole point — scan the code on the counter, the catalog opens.
> No app, no signup, no typing a URL.

### Q&A you should be ready for
- *"What happens to the QR code if a shop is deactivated?"* → The code still exists but the catalog returns unavailable. Reactivating restores it — the QR never needs reprinting.
- *"Can a customer place an order?"* → Not by design. The shop's phone number is on the page; the transaction happens the way it already does, in the shop or over the phone.
- *"Is the public catalog fast?"* → Images are lazy-loaded and cached for 30 days by nginx, and the whole catalog is a single API call.

---

## Phase 5 — Subscriptions, Deployment & Future Scope
**Presenter 5 · 5 minutes**

[SLIDE: business model]

> I'll cover how this works as a product, and how it's deployed.
>
> ScanStore is a subscription. Every shop gets **15 days free**, no card
> required. After that:

[SLIDE: pricing table]

> **₹299 a month. ₹1,199 for six months. Or ₹1,999 for a year.**
>
> Those numbers are deliberate. Our buyer already pays roughly ₹1,000 to ₹3,000
> a year for billing software like Vyapar — that's the price band he's accepted.
> At ₹1,999 a year, a shop covers the whole subscription by selling **one extra
> saree**. And the annual plan is 44% cheaper per month than monthly, which is
> what pushes people onto it.
>
> Payments go through **Razorpay**, which matters because this market pays by
> UPI. We take a one-time payment per term rather than an auto-debit mandate —
> small merchants don't trust recurring auto-debit, and mandate setup has a high
> drop-off.

[SLIDE: deployment]

> On deployment. The whole stack runs in Docker — three containers: nginx serving
> React, the .NET API, and MySQL. Data lives in **named volumes**, so containers
> can be destroyed and rebuilt without losing anything. We tested that by tearing
> the containers down completely and bringing them back.
>
> Configuration comes from a single `.env` file. No passwords in the source code
> — we removed them, because that file is committed to git.
>
> And deployment is one command:

[SLIDE: deploy script]

> Our deploy script validates the configuration, **backs up the database**,
> pulls, rebuilds, waits for the health check, and smoke-tests the site. If
> anything fails it stops and tells you how to roll back.
>
> One guard in it is worth mentioning: it refuses to deploy if the public URL is
> still set to `localhost`. That's not hypothetical — it caught exactly that
> mistake on our live server, where every QR code would have pointed customers at
> their own phone.

[SLIDE: limitations — be honest here]

> Current limitations, honestly. We're running on an IP address over HTTP, not
> HTTPS, so this isn't production-ready for real payments yet — that needs a
> domain and a certificate. And the QR codes encode the site address, so moving
> to a domain requires regenerating them; we automated that, the API rewrites
> stale codes on startup.

[SLIDE: future scope]

> Future scope: WhatsApp order buttons on each product, multi-outlet support for
> shops with more than one branch, staff logins with limited permissions, and
> analytics on which products customers actually view.

[SLIDE: conclusion]

> To summarise — we built a platform that puts a local shop online in ten
> minutes, for less than the cost of one sale a year, and requires nothing from
> the customer but a phone camera.
>
> It's live now at **46.225.136.102**. Thank you — we're happy to take questions.

### Q&A you should be ready for
- *"Is the payment integration real or a mock?"* → Real Razorpay integration — order creation, signature verification, and a webhook endpoint. It runs in test mode; going live is a key swap.
- *"How is it secured?"* → Every API endpoint requires a verified Firebase token except the public catalog. Vendors are resolved from the token, so you can't request another vendor's data. The database isn't exposed to the internet. The honest gap is HTTPS, which needs a domain.
- *"How would this scale?"* → The API is stateless, so it scales horizontally behind a load balancer. The bottleneck would be uploaded files on local disk — that moves to object storage. We already store relative paths, so that change doesn't touch the database.
- *"What was the hardest part?"* → Naming a real one is better than "everything". Good answers: the SQL Server → MySQL migration for ARM; getting validation consistent across client and server; or making deployment repeatable after a misconfigured URL broke the live site.

---

## Timing cheat sheet

| Phase | Presenter | Time | Demo? |
|---|---|---|---|
| 1 — Problem & objectives | 1 | 5 min | No |
| 2 — Architecture & data model | 2 | 5 min | No |
| 3 — Vendor module | 3 | 5 min | **Yes** |
| 4 — Admin & customer | 4 | 5 min | **Yes** |
| 5 — Business, deployment, future | 5 | 5 min | Slides |
| Q&A | all | 5 min | — |

## Rules for the demo

1. **One laptop, one browser, one driver.** Presenters 3 and 4 both demo — don't swap machines mid-flow.
2. **Seed data first.** Two shops, several products, at least one with multiple images. An empty catalog is a terrible demo.
3. **Log in before you start.** Have vendor and admin sessions already open in tabs.
4. **Never say "it should work".** If a demo fails, switch to the backup video and keep talking.
5. **Don't oversell.** Everything in this script is built. Keep it that way.

# Context & Product Overview
### School Lost-and-Found Management System

> This section is for first-time readers; it helps you grasp the **problem** and the **high-level solution** before diving into the project's technical details.

---

## 1. Context

In schools and universities — where thousands of people move every day between lecture halls, libraries, canteens, and sports grounds — dropping and losing belongings happens constantly. From small things like student ID cards, keys, and earphones, to valuables such as wallets, phones, and laptops. Most schools today handle this in a **manual and fragmented** way: the finder brings the item to a security guard or proctor, the item gets put away in some drawer, and the person who lost it has to go around asking at each location to try to find it again.

This approach has existed for a long time but has never truly been effective. There is no central place to look things up, no catalogue of items currently held, and most importantly **no information link** between the person who lost an item and the person who found it. As a result, many items — even after being handed in — never make it back to their rightful owner, eventually piling up and being thrown away.

---

## 2. Problem (pain points)

Looking at it from the perspective of each person in the story, the problems become clear:

**The person who lost an item** does not know where to start. They run back and forth between the security office, the faculty office, and the proctor team — each place answering differently, mostly with "no one has handed anything in yet." They have no way of knowing whether their item **has already been found and handed in or not**, so they often give up early even though the item is in fact still sitting in storage.

**The person who found an item** is reluctant to bother. Having found a card or a pair of earphones, they are not sure where to hand it in, and once handed in, they have no idea whether the item ever reaches its owner. This lack of feedback leads many people to simply… leave it be, losing the goodwill they started with.

**The receiving staff (security, proctors)** end up holding an ever-growing pile of unclaimed items with no tool to manage them. No catalogue, no categorization; whenever someone comes asking, they rummage by hand to look. Items held for a long time are prone to damage and further loss, and the staff also have no way to verify who is truly the owner of an item.

**The school** has no data at all: it does not know how many items are lost each term, what the return rate is, or how items pile up — and therefore cannot improve the process.

> The crux: the biggest problem is **not** "where to store the item" (the security staff can already do that), but the **breakdown of information between the loser and the finder**, together with the **lack of a transparent, verified return process**.

---

## 3. Why a digital solution is needed

All of the obstacles above come down to one thing: **the absence of a central point for information and a clear process**. This is exactly where software delivers the most value — not to replace people in storing items, but to:

- Gather all lost / found information into **a single place** that everyone can search.
- **Automatically match** lost-item reports against items currently in storage, instead of relying on people to remember and compare manually.
- **Proactively notify** the loser the moment a matching item appears, instead of leaving them to ask around.
- Constrain the return process into controlled steps, **preventing mistaken claims and disputes**.

---

## 4. Solution overview

The product is a **web application for managing lost-and-found at schools**. But it must be emphasized: this is **not** a mere "online storage box." More accurately, it is a **lost/found information matching exchange, tied to a controlled return process**.

Operating philosophy: **trust the finder, reduce red tape**. By default the finder keeps the item themselves and returns it directly to the owner, with no mandatory routing through staff — because someone with bad intentions would have walked off with the item from the start rather than posting it. Staff step in only when needed: holding high-value items, or arbitrating when there is a dispute.

How it works at a high level:

1. **The person who found an item** posts a listing, describing it publicly just enough (type of item, where/when it was found) while withholding the distinctive details as a verification "key." By default they keep the item themselves; if it is valuable, they may hand it to staff for safekeeping.
2. **The person looking for a lost item** does not have to sit watching the list — they **subscribe to a watch-alert** by area, time range, item type, and keywords. When a new item matching those criteria appears, **the system automatically sends a realtime notification** to the right person.
3. The person who believes the item is theirs submits a **claim** along with verification information (correctly describing the withheld details, or proof of ownership).
4. **The holder** checks the evidence and then accepts; the two parties arrange to meet, and **both confirm** the handover in the system to close the lifecycle — enough to leave a transparent trail without slowing anyone down.
5. After receiving the item, the owner can **send a thank-you** to the finder. Users can also **request the security team to extract camera footage** for the relevant area when needed.
6. **The administrator** monitors everything through a statistics dashboard, handles expired unclaimed items, and views the log of every action to ensure transparency.

---

## 5. User audiences

| Audience | Role in the system |
|---|---|
| **Guest** | Not logged in; browses the public list of found items. |
| **Member** | Student / lecturer; reports found items, subscribes to watch-alerts for lost items, submits & handles claims, confirms handovers, thanks the finder, requests camera checks. |
| **Staff** | Security / proctor; holds high-value items, arbitrates disputes, handles camera footage extraction requests. |
| **Admin** | Manages users, categories, statistics, and oversees the whole system. |

---

## 6. Core value

Unlike an ordinary "stock-in/stock-out" piece of software, the real value of the system lies in three points:

1. **Watch-alert-style matching (Matching)** — the searcher subscribes to a filter by area, time, item type, and keywords; when a new item matches, the system notifies automatically, with no need to sit watching the list.
2. **A verified return process** — each item passes through a clear lifecycle; the claimant must correctly describe the item's withheld details, and the handover requires both parties to confirm, limiting mistaken claims and disputes.
3. **Proactive, instant notifications** — the searcher is notified as soon as a matching item appears, instead of waiting or asking around.

---

## 7. Product scope

**In scope:**
- Reporting found items and managing the lifecycle of each item.
- Subscribing to watch-alerts for lost items and automatic matching (publish/subscribe).
- The process for claims, verification, and two-way confirmed handover.
- Camera footage extraction requests (a channel for submitting requests + receiving responses from security).
- Thanking / rating the finder after an item is returned.
- In-app notifications (persisted + realtime).
- Statistics and a system log (including a public timeline on each item).

**Out of scope (at this stage):**
- Payments / monetary rewards through the system.
- Hardware integration (automatic cameras, sensors, scanning of item identification codes) — the camera request is only an information channel; viewing footage is done manually by security.
- A dedicated mobile app (only a responsive web build).
- Sending email / SMS outside the system (may be considered later).

---

## 8. Goals

- **Shorten the time** from when an item is handed in to when it reaches its owner.
- **Increase the successful return rate** compared with the manual approach.
- **Reduce the load** on receiving staff through automatic search and matching.
- **Make transparent** the entire process via a log and a clear verification procedure.

> The detailed technical part (architecture, data model, state lifecycle, technologies used, etc.) is presented in the `PROJECT_INSTRUCTION.md` document. The requirements and work assignments are in `REQUIREMENTS_2DEV.md`.

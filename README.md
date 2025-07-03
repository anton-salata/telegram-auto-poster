# Telegram Auto Poster

🛰️ **Telegram Auto Poster**  
A fully automated content delivery tool that scrapes selected websites and posts updates directly to Telegram channels — without any human interaction.

---

## 🚀 Overview

**Telegram Auto Poster** is a background service built with .NET that continuously monitors and scrapes news from selected websites and automatically publishes updates to Telegram channels.

This tool operates fully autonomously — no manual input or moderation required. Designed to keep niche channels fresh with the latest news 24/7.

---

## ✅ Features

- 🕵️ Scrapes content from multiple websites
- 📡 Posts new articles to Telegram channels automatically
- 🧠 Deduplicates content using SQLite
- 🧰 Uses .NET hosting infrastructure for clean background execution
- 🔄 Fully automated — zero human interaction

---

## 🌐 Configured Feeds

| Scraper ID  | Source Website                                          | Telegram Channel         |
|-------------|---------------------------------------------------------|---------------------------|
| `CarNews`   | https://www.thedrive.com/category/news                  | [@carnewsdaily](https://t.me/carnewsdaily)       |
| `AlienWire` | https://www.coasttocoastam.com/inthenews/              | [@alienwireufo](https://t.me/alienwireufo)       |
| `BmwNews`   | https://www.bmwblog.com/category/bmw-news/             | [@bmwnewsdaily](https://t.me/bmwnewsdaily)       |

---

## 🧩 Tech Stack

- **C# (.NET 9)**
- `HtmlAgilityPack` for HTML parsing
- `Telegram.Bot` for Telegram API access
- `SQLite` for local state tracking
- `Microsoft.Extensions.Hosting` for background service
- `Microsoft.Extensions.Logging` for structured logging

# Návod k nasazení

Tento návod popisuje nasazení aplikace Výkazy z pohledu samotného repozitáře. Neřeší instalaci Linux serveru, nastavení DNS, firewallu ani správu infrastruktury. Předpokládá se, že na cílovém serveru už funguje Docker Compose a že veřejná doména je připravená.

## Co se spouští

Produkční nasazení používá soubor `docker-compose.prod.yml`.

| Služba | Účel |
| --- | --- |
| `timesheets.database` | PostgreSQL databáze. Data jsou uložená v Docker volume `timesheets_db`. |
| `timesheets.backend` | ASP.NET Core API na interním portu `5000`. Při startu aplikuje databázové migrace. |
| `timesheets.frontend` | Sestavený React frontend obsluhovaný nginxem uvnitř kontejneru. |
| `timesheets.nginx` | Veřejná reverse proxy. Směruje frontend, API, autentizaci a notifikace. |
| `timesheets.certbot` | Vydání a obnova Let's Encrypt certifikátu pro doménu z `PUBLIC_HOST`. |

Image se staví lokálně ze zdrojového kódu v repozitáři. Výjimkou je databáze, která používá hotový image `postgres:17-alpine`.

## 1. Stažení aplikace

Na serveru naklonujte repozitář:

```bash
git clone https://github.com/ondrejsvorc/Timesheets.git
cd Timesheets
```

Pokud se nasazuje jiná větev než `main`, přepněte ji podle jejího názvu:

```bash
git checkout [nazev-vetve]
```

## 2. Vytvoření `.env`

Soubor `.env` obsahuje konfiguraci konkrétního nasazení. Necommituje se do Gitu, protože obsahuje hesla a tajné klíče.

Vytvořte ho ze vzoru:

```bash
cp .env.example .env
nano .env
```

Význam proměnných:

| Proměnná | Význam |
| --- | --- |
| `POSTGRES_PASSWORD` | Heslo databázového uživatele `postgres`. Po prvním spuštění ho neměňte bez řízené migrace databáze. |
| `PUBLIC_HOST` | Veřejná doména aplikace bez `https://`. Používá ji nginx i certbot. |
| `LETSENCRYPT_EMAIL` | E-mail pro registraci a obnovu Let's Encrypt certifikátu. |
| `AUTHENTICATION__CLIENTID` | Identifikátor OIDC klienta získaný od poskytovatele identity. Nemusí být stejný jako `PUBLIC_HOST`. |
| `AUTHENTICATION__CLIENTSECRET` | Tajný klíč OIDC klienta. |
| `AUTHENTICATION__METADATAADDRESS` | Adresa OIDC metadata dokumentu. Výchozí hodnota míří na UJEP IdP. |
| `AUTHENTICATION__ISSUER` | Očekávaný issuer OIDC poskytovatele. |
| `ADMINISTRATION__ROLEMANAGERPERSONALNUMBERS__0` | Osobní číslo prvního uživatele, který může spravovat globální role. Další správci se přidávají jako `...__1`, `...__2` atd. |

Po vytvoření omezte práva k souboru:

```bash
chmod 600 .env
```

Soubor `.env` obsahuje databázové heslo a OIDC secret. Práva `600` zajistí, že ho může číst a upravovat pouze vlastník souboru, ne ostatní uživatelé serveru.

## 3. Kontrola konfigurace

Před spuštěním nechte Docker Compose složit výslednou konfiguraci:

```bash
docker compose -f docker-compose.prod.yml config
```

Pokud chybí povinná proměnná, příkaz skončí chybou. V takovém případě opravte `.env` a spusťte kontrolu znovu.

## 4. Spuštění aplikace

Sestavte image a spusťte kontejnery:

```bash
docker compose -f docker-compose.prod.yml up -d --build
```

Zkontrolujte stav:

```bash
docker compose -f docker-compose.prod.yml ps
```

Zobrazte logy:

```bash
docker compose -f docker-compose.prod.yml logs -f
```

Při prvním startu backend automaticky aplikuje databázové migrace. Testovací data se v produkčním režimu nevkládají.

## 5. Ověření nasazení

Ověřte API:

```bash
curl -fsS https://nasazena-aplikace.cz/api/health
```

Očekávaný výstup:

```text
Healthy
```

Potom otevřete aplikaci v prohlížeči:

```text
https://nasazena-aplikace.cz/
```

Ověřte přihlášení přes OIDC a přístup prvního správce systému.

## 6. Nasazení nové verze

Přejděte do adresáře aplikace:

```bash
cd Timesheets
```

Stáhněte poslední změny:

```bash
git pull --ff-only
```

Před deployem vytvořte zálohu databáze:

```bash
mkdir -p backups
docker compose -f docker-compose.prod.yml exec -T timesheets.database \
  pg_dump -U postgres -d timesheets -Fc \
  > "backups/timesheets_$(date +%Y%m%d_%H%M%S).dump"
```

Nasaďte novou verzi:

```bash
docker compose -f docker-compose.prod.yml up -d --build
```

Ověřte stav:

```bash
docker compose -f docker-compose.prod.yml ps
curl -fsS https://vykazy.ujep.cz/api/health
```

## 7. Důležité poznámky

- `.env` nikdy necommitujte.
- `POSTGRES_PASSWORD` po vytvoření databázového volume neměňte bez řízeného postupu.
- Nepoužívejte `docker compose down -v`, pokud nechcete smazat databázi a uložené certifikáty.
- Doména se nemění v nginx šablonách, ale pouze přes `PUBLIC_HOST` v `.env`.

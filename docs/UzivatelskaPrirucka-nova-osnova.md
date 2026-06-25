# Uživatelská příručka

## O aplikaci

Webová aplikace Výkazy slouží ke správě pracovních výkazů a souvisejících údajů o univerzitních projektech, zakázkách a pracovních pozicích.

## Uživatelské role a oprávnění

Aplikace zobrazuje své jednotlivé části podle role přihlášeného uživatele. Každý uživatel má přístup ke svým vlastním pracovním výkazům. Další oprávnění získává podle toho, zda dále působí jako manažer zakázky, manažer projektu nebo globální manažer.
| Role | Popis |
|---|---|
| Zaměstnanec | Může si zobrazit své výkazy, nahrát docházku z IMIS, upravit svůj výkaz a odeslat jej ke schválení a také jej finálně schvalovat. |
| Manažer zakázky | Může pracovat se zakázkami, ke kterým je přiřazen. Vidí výkazy zaměstnanců přiřazených k dané zakázce a schvaluje zakázkové části výkazu. |
| Manažer projektu | Může spravovat projekty, ke kterým je přiřazen. Může spravovat zakázky v rámci projektu a přiřazovat manažery zakázek. |
| Globální manažer | Má přístup ke všem projektům, zakázkám, zaměstnancům a výkazům. Může vytvářet projekty a provádět finální schválení výkazů. |
| Správce systému | Spravuje technická nebo systémová oprávnění uživatelů. Tato role není určena pro běžnou práci s výkazy. |

### Typ zaměstnance Akademik / Neakademik

Kromě uživatelských rolí aplikace rozlišuje také typ zaměstnance: **Akademik** a **Neakademik**. Tento údaj neurčuje, jaké části aplikace může uživatel spravovat, ale ovlivňuje způsob práce s výkazem.

Typ zaměstnance se do aplikace přenáší automaticky při přihlášení podle údajů ze systému IMIS. Uživatel jej nemůže ručně změnit. Pokud se typ zaměstnance v IMIS změní, aplikace změnu zohlední při dalším přihlášení uživatele. Již vytvořené výkazy se tím ale zpětně nezmění. Každý výkaz si při svém vytvoření uloží aktuální typ zaměstnance jako historický údaj. Díky tomu zůstane výkaz vyhodnocen podle pravidel platných v době jeho vytvoření a pozdější změna typu zaměstnance nijak neovlivní jeho obsah.

Rozdíl mezi typy zaměstnance je důležitý hlavně při práci s docházkou a výkazem. U neakademických pracovníků se pracuje s docházkou podle nahraných časů a pravidel pro pracovní dobu. U akademických pracovníků se výkaz vyhodnocuje odlišně, protože jejich pracovní režim není být založen na běžné evidenci příchodů a odchodů.

## Přístup do aplikace

### Přihlášení

Pro přihlášení do aplikace zadejte své přihlašovací údaje do systému IMIS (uživatelské jméno a heslo).

![Přihlášení](./images/v-00-01-prihlaseni.png)

### Odhlášení

Pro odhlášení klikněte na ikonu profilu a v zobrazené nabídce zvolte možnost *Odhlásit se*.

![Odhlášení](./images/v-00-02-odhlaseni.png)

## Projekty

[KRÁTKÝ TEXT O TOMTO MODULU - NE KAŽDÝ HO VIDÍ, PROČ EXISTUJE A TAK. KRÁTCE.]

![Stránka Projekty](./images/v-01-00-00-projekty.png)

### Vytvoření projektu

...
![Tlačítko vytvořit projekt](./images/v-01-00-91-projekty-sipka-vytvorit.png)
![Dialog pro vytvoření nového projektu](./images/v-01-00-01-projekty-modal-vytvorit.png)

### Úprava projektu

...
![Stránka projekty- otevření nabídky](./images/v-01-00-92-projekty-sipka-nabidka.png)
![Stránka Projekty - nabídka akcí pro daný projekt](./images/v-01-00-00-projekty-nabidka.png)
![Dialog pro úpravu projektu](./images/v-01-00-02-projekty-modal-upravit.png)

...

### Přidání manažera projektu

...
![Rozkliknutí detailu projektu](./images/v-01-00-93-projekty-sipka-projekt.png)
![Stránka detailu projektu - překliknutí záložky](./images/v-01-01-92-projekt-zakazky-sipka-zalozka-pr.manazeri.png)
![Stránka detailu projektu - záložka Manažeři projektu](./images/v-01-02-00-projekt-pr.manazeri.png)
![Dialog pro přidání projektového manažera](./images/v-01-02-01-projekt-pr.manazeri-modal-pridat.png)

## Zakázky

![Rozkliknutí detailu projektu](./images/v-01-00-93-projekty-sipka-projekt.png)
![Stránka detailu projektu - záložka Zakázky](./images/v-01-01-00-projekt-zakazky.png)

### Vytvoření zakázky

![Tlačítko vytvořit zakázku](./images/v-01-01-91-projekt-zakazky-sipka-vytvorit.png)
![Dialog pro vytvoření nové zakázky](./images/v-01-01-01-projekt-zakazky-modal-pridat.png)

### Úprava zakázky

### Přidání manažera zakázky

![Stránka detailu projektu - překliknutí záložky](./images/v-01-01-93-projekt-zakazky-sipka-zalozka-zak.manazeri.png)
![Stránka detailu projektu - záložka Manažeři zakázek](./images/v-01-03-00-projekt-zak.manazeri.png)
![Dialog pro přidaní manažera zakázky](./images/v-01-03-01-projekt-zak.manazeri-modal-pridat.png)

### Přidání zaměstnance do zakázky

![Rozkliknutí detailu dané zakázky](./images/v-01-01-94-projekt-zakazky-sipka-zakazka.png)
![Stránka detailu zakázky - překliknutí záložky](./images/v-02-00-91-zakazka-vykazy-sipka-zalozka_zamestnanci.png)
![Stránka detailu zakázky - záložka Zaměstnanci](./images/v-02-01-92-zakazka-zamestnanci-sipka-vytvorit.png)
![Dialog pro přidání pozice v zakázce](./images/v-02-01-01-zakazka-zamestnanci-modal-pridat.png)

## Zaměstnanci

### Vytvoření pracovní pozice

### Úprava pracovní pozice

### Přehled pracovních pozic

## Docházka

### Export docházky z IMIS
[ZDE SCREENSHOTY Z IMIS JAK TEN VÝKAZ SPRÁVNĚ VYEXPORTOVAT]

### Nahrání docházky z IMIS
V současné době není aplikace propojena se systémem IMIS. Docházku je proto nutné nejprve exportovat ze systému IMIS a následně ji nahrát do aplikace Výkazy.

[OBRÁZEK ZDE]

## Výkazy

### Úprava výkazu

### Odeslání výkazu ke schválení

### Schválení výkazu

### Vrácení výkazu k přepracování

### Historie schvalování

## Otázky a odpovědi

**Co se stane, když není vyplněno datum ukončení projektu?**

...

**Lze datum ukončení projektu změnit?**

...


**Může být zaměstnanec přiřazen do více zakázek současně?**

...

**Lze zaměstnance ze zakázky odebrat?**

...

**Jaký je rozdíl mezi akademickým a neakademickým pracovníkem?**

...

**Kdo může upravovat údaje zaměstnance?**

...

**Kdo může výkaz schválit?**

...

**Lze upravovat již schválený výkaz?**

...

**Jak poznám, že byl výkaz vrácen k úpravě?**

...
# Uživatelská příručka

## O aplikaci

Webová aplikace Výkazy slouží ke správě pracovních výkazů a souvisejících údajů o univerzitních projektech, zakázkách a pracovních pozicích.

## Uživatelské role a oprávnění

Aplikace zobrazuje své jednotlivé části podle role přihlášeného uživatele. Každý uživatel má přístup ke svým vlastním pracovním výkazům. Další oprávnění získává podle toho, zda dále působí jako manažer zakázky, manažer projektu nebo globální manažer.
| Role | Popis |
|---|---|
| Zaměstnanec | Může si zobrazit své výkazy, nahrát docházku z IMIS, upravit svůj výkaz, odeslat jej ke schválení a po schválení projektových částí provést finální schválení vlastního výkazu. |
| Manažer zakázky | Může pracovat se zakázkami, ke kterým je přiřazen. Vidí výkazy zaměstnanců přiřazených k dané zakázce a schvaluje zakázkové části výkazu. |
| Manažer projektu | Může spravovat projekty, ke kterým je přiřazen. Může spravovat zakázky v rámci projektu a přiřazovat manažery zakázek. |
| Globální manažer | Má přístup ke všem projektům, zakázkám, zaměstnancům a výkazům. Může vytvářet projekty a provádět finální schválení výkazů. |
| Správce systému | Spravuje technická nebo systémová oprávnění uživatelů. Tato role není určena pro běžnou práci s výkazy. |

### Typ zaměstnance Akademik / Neakademik

Kromě uživatelských rolí aplikace rozlišuje také typ zaměstnance: **Akademik** a **Neakademik**. Tento údaj neurčuje, jaké části aplikace může uživatel spravovat, ale ovlivňuje způsob práce s výkazem.

Typ zaměstnance se do aplikace přenáší automaticky při přihlášení podle údajů ze systému IMIS. Uživatel jej nemůže ručně změnit. Pokud se typ zaměstnance v IMIS změní, aplikace změnu zohlední při dalším přihlášení uživatele. Již vytvořené výkazy se tím ale zpětně nezmění. Každý výkaz si při svém vytvoření uloží aktuální typ zaměstnance jako historický údaj. Díky tomu zůstane výkaz vyhodnocen podle pravidel platných v době jeho vytvoření a pozdější změna typu zaměstnance nijak neovlivní jeho obsah.

Rozdíl mezi typy zaměstnance je důležitý hlavně při práci s docházkou a výkazem. U neakademických pracovníků se pracuje s docházkou podle nahraných časů a pravidel pro pracovní dobu. U akademických pracovníků se výkaz vyhodnocuje odlišně, protože jejich pracovní režim není být založen na běžné evidenci příchodů a odchodů.

## Jak aplikace pracuje s docházkou z IMIS

Aplikace zatím není přímo napojena na IMIS. Docházku je proto nutné nejprve exportovat z IMIS do souboru `.xls` nebo `.xlsx` a tento soubor následně nahrát do aplikace. Po nahrání aplikace ze souboru přečte potřebná data a uloží si je. Další úpravy už probíhají přímo v aplikaci. Pokud je docházka z IMIS exportována znovu, lze ji znovu nahrát pouze u výkazu ve stavu *Rozpracovaný*.

### Doporučený postup práce s výkazem
1. Zaměstnanec exportuje docházku z IMIS za daný měsíc.
1. Zaměstnanec nahraje docházku z IMIS za daný měsíc do aplikace.
1. Zaměstnanec zkontroluje docházku a doplní rozdělení hodin mezi kmenovou a projektovou část.
1. Pokud pracovní výkaz neobsahuje chyby, zaměstnanec jej odešle ke schválení.
1. Manažeři zakázek nebo projektů schválí projektové části výkazu.
1. Po schválení všech projektových částí zaměstnanec svůj pracovní výkaz finálně schválí.

## Přístup do aplikace

### Přihlášení

Pro přihlášení do aplikace zadejte své přihlašovací údaje do systému IMIS (uživatelské jméno a heslo).

![Přihlášení](./images/v-00-01-prihlaseni.png)

### Odhlášení

Pro odhlášení klikněte na ikonu profilu a v zobrazené nabídce zvolte možnost *Odhlásit se*.

![Odhlášení](./images/v-00-02-odhlaseni.png)

## Projekty

Modul Projekty slouží ke správě univerzitních projektů. Projekt představuje nejvyšší organizační celek, pod který jsou následně zařazeny jednotlivé zakázky. U každého projektu lze evidovat základní identifikační údaje, dobu jeho platnosti a odpovědné osoby.

![Stránka Projekty](./images/v-01-00-00-projekty.png)

### Vytvoření projektu

![Tlačítko vytvořit projekt](./images/v-01-00-91-projekty-sipka-vytvorit.png)
![Dialog pro vytvoření nového projektu](./images/v-01-00-01-projekty-modal-vytvorit.png)

### Úprava projektu

![Stránka projekty- otevření nabídky](./images/v-01-00-92-projekty-sipka-nabidka.png)
![Stránka Projekty - nabídka akcí pro daný projekt](./images/v-01-00-00-projekty-nabidka.png)
![Dialog pro úpravu projektu](./images/v-01-00-02-projekty-modal-upravit.png)

### Přidání manažera projektu

![Rozkliknutí detailu projektu](./images/v-01-00-93-projekty-sipka-projekt.png)
![Stránka detailu projektu - překliknutí záložky](./images/v-01-01-92-projekt-zakazky-sipka-zalozka-pr.manazeri.png)
![Stránka detailu projektu - záložka Manažeři projektu](./images/v-01-02-00-projekt-pr.manazeri.png)
![Dialog pro přidání projektového manažera](./images/v-01-02-01-projekt-pr.manazeri-modal-pridat.png)

## Zakázky

Modul Zakázky představují dílčí části projektů, ke kterým jsou přiřazováni zaměstnanci a ve kterých následně vykazují odpracovaný čas. Každá zakázka je vždy součástí konkrétního projektu a může mít vlastního manažera odpovědného za schvalování vykázané práce.

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

Modul Zaměstnanci slouží ke správě pracovních pozic zaměstnanců v jednotlivých zakázkách. U každé pozice lze evidovat období platnosti, rozsah úvazku a další údaje potřebné pro správné vytváření a vyhodnocování pracovních výkazů.

### Vytvoření pracovní pozice

### Úprava pracovní pozice

### Přehled pracovních pozic

## Docházka

Modul Docházka slouží k nahrání docházkových dat exportovaných ze systému IMIS. Nahraná data představují podklad pro vytváření pracovních výkazů a kontrolu vykázané pracovní doby.

### Export docházky z IMIS
[ZDE SCREENSHOTY Z IMIS JAK TEN VÝKAZ SPRÁVNĚ VYEXPORTOVAT]

### Nahrání docházky z IMIS
V současné době není aplikace propojena se systémem IMIS. Docházku je proto nutné nejprve exportovat ze systému IMIS a následně ji nahrát do aplikace Výkazy.

[OBRÁZEK ZDE]

## Výkazy

Modul Výkazy slouží k evidenci, úpravám a schvalování pracovních výkazů zaměstnanců. Uživatel zde může sledovat stav jednotlivých výkazů, doplňovat vykázanou práci a pracovat s připomínkami vzniklými během schvalovacího procesu.

### Úprava výkazu

### Odeslání výkazu ke schválení

### Schválení výkazu

### Vrácení výkazu k přepracování

### Historie schvalování

## Otázky a odpovědi

**Co se stane, když není vyplněno datum ukončení projektu?**

Projekt zůstává aktivní až do okamžiku, kdy je datum ukončení doplněno. Datum ukončení lze doplnit později.

Příklad: projekt začíná `1. 1. 2026` a datum ukončení není vyplněno. K projektu lze dál vytvářet zakázky a pracovní pozice i po roce 2026, dokud není projekt ukončen.

**Lze datum ukončení projektu změnit?**

Ano. Datum ukončení projektu lze upravit, ale nelze jej zkrátit tak, aby existující pracovní pozice na jeho zakázkách začaly být mimo nové období projektu.

Příklad: projekt končí `31. 12. 2026` a zaměstnanec má na jeho zakázce pozici do `30. 9. 2026`. Projekt nelze zkrátit na `30. 6. 2026`, ale lze jej zkrátit na `30. 9. 2026` nebo později.

**Může být zaměstnanec přiřazen do více zakázek současně?**

Ano. Zaměstnanec může být přiřazen do více zakázek, pokud se tím nepřekročí jeho celkový úvazek pro dané období.

Příklad: zaměstnanec má celkový úvazek `1,0`. Může mít současně `0,4` na jedné zakázce a `0,3` na druhé. Další přiřazení `0,4` už aplikace nepovolí, protože součet by byl `1,1`.

**Lze zaměstnance ze zakázky odebrat?**

Ano, ale pouze pokud daná pozice nemá navázané výkazy ve stavu *Ke schválení* nebo *Schválený*. Pokud takové výkazy existují, odebrání není povoleno.

Příklad: zaměstnanec má na zakázce pozici za březen 2026 a březnový výkaz je již schválený. Pozici nelze odebrat, protože by se tím narušil již schválený výkaz.

**Kdo může výkaz schválit?**

Projektové části výkazu schvaluje manažer příslušné zakázky nebo projektu. Finální schválení celého výkazu je možné až po schválení všech projektových (resp. zakázkových) částí a provádí jej buď sám zaměstnanec, nebo globální manažer.

Příklad: výkaz obsahuje práci na dvou zakázkách. Každou projektovou část musí nejprve schválit odpovědný manažer. Teprve potom lze schválit celý výkaz.

**Lze upravovat již schválený výkaz?**

Ne přímo. Schválený výkaz je uzamčený. Pokud je potřeba oprava, musí být výkaz nejprve vrácen do rozpracovaného stavu.

Příklad: pokud se po schválení zjistí chyba, nelze ji opravit rovnou ve schváleném výkazu. Výkaz je nutné vrátit k úpravě, opravit a znovu projít schválením.

**Jak poznám, že byl výkaz vrácen k úpravě?**

Výkaz se vrátí do stavu *Rozpracovaný*. Uživatel zároveň vidí záznam v historii schvalování, případně komentář s důvodem vrácení.

**Co znamená stav Rozpracovaný?**

Výkaz je možné upravovat a ještě nebyl odeslán ke schválení.

Příklad: zaměstnanec právě nahrál docházku z IMIS a doplňuje rozdělení hodin mezi zakázky. Výkaz je zatím rozpracovaný.

**Co znamená stav Ke schválení?**

Výkaz byl odeslán a čeká na kontrolu manažerů. Běžné úpravy už nejsou povolené, dokud není výkaz vrácen k úpravě.

Příklad: zaměstnanec dokončí výkaz za leden 2026 a odešle jej. Manažeři zakázek poté kontrolují projektové části výkazu.

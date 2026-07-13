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

![Odhlášení](./images/v-00-02-odhlaseni-sipky.png)

## Projekty

Modul Projekty slouží ke správě univerzitních projektů. Projekt představuje nejvyšší organizační celek, pod který jsou následně zařazeny jednotlivé zakázky. U každého projektu lze evidovat základní identifikační údaje, dobu jeho platnosti a odpovědné osoby.

![Stránka Projekty](./images/v-01-00-20-projekty-sipka-navbar.png)

### Vytvoření projektu

Vytvoření projektu může provést uživatel s rolí *Globálního manažera* kliknutím na tlačítko *Vytvořit projekt* na stránce projektů.

![Stránka Projekty - Tlačítko Vytvořit projekt](./images/v-01-00-21-projekty-sipka-vytvorit.png)
![Dialog pro vytvoření nového projektu](./images/v-01-00-05-projekty-modal-vytvorit.png)

### Úprava projektu

Upravit údaje projektu může provést uživatel s rolí *Globálního manažera* nebo *Manažera projektu* kliknutím na *Upravit* v nabídce akcí u daného projektu.

![Stránka Projekty - otevření nabídky](./images/v-01-00-23-projekty-sipka-tri_tecky.png)
![Stránka Projekty - nabídka - upravit](./images/v-01-00-24-projekty-sipka-upravit.png)
![Dialog pro úpravu projektu](./images/v-01-00-06-projekty-modal-upravit.png)

### Přidání manažera projektu

Přidat manažera k projektu může provést uživatel s rolí *Globálního manažera* kliknutím na *Přidat manažera* na stránce detailu projektu, v záložce *Manažeři projektu*.

![Rozkliknutí detailu projektu](./images/v-01-00-22-projekty-sipka-projekt.png)
![Stránka detailu projektu - překliknutí záložky](./images/v-01-01-24-projekt-zakazky-sipka-zalozka-manazeri_projektu.png)
![Stránka detailu projektu - záložka Manažeři projektu](./images/v-01-02-20-projekt-manazeri_projektu-sipka-vytvorit.png)
![Dialog pro přidání projektového manažera](./images/v-01-02-00-projekt-manazeri_projektu-modal-pridat.png)

### Archivace projektu

Archivovat projekt může uživatel s rolí *Globálního manažera* nebo *Manažera projektu* kliknutím na *Archovovat* v nabídce akcí daného projektu.

![Stránka Projekty - otevření nabídky](./images/v-01-00-23-projekty-sipka-tri_tecky.png)
![Stránka Projekty - nabídka - archivovat](./images/v-01-00-25-projekty-sipka-archivovat.png)

### Obnovení projektu z archivu

Obnovit projekt může uživatel s rolí *Globálního manažera* nebo *Manažera projektu* kliknutím na *Obnovit z archivu* v nabídce akcí daného projektu.

![Stránka Projekty - filtrovat projekty - archivované](./images/v-01-00-27-projekty-sipka-filtr_archovovane.png)
![Stránka Projekty - nabídka archivovaného projektu](./images/v-01-00-28-projekty-sipka-archovovane_obnovit.png)

### Smazání projektu

Smazat projekt může uživatel s rolí *Globálního manažera* nebo *Manažera projektu* kliknutím na *Smazat* v nabídce akcí daného projektu.

![Stránka Projekty - otevření nabídky](./images/v-01-00-23-projekty-sipka-tri_tecky.png)
![Stránka Projekty - nabídka - smazat](./images/v-01-00-26-projekty-sipka-smazat.png)


## Zakázky

Modul Zakázky představují dílčí části projektů, ke kterým jsou přiřazováni zaměstnanci a ve kterých následně vykazují odpracovaný čas. Každá zakázka je vždy součástí konkrétního projektu a může mít vlastního manažera odpovědného za schvalování vykázané práce.

![Rozkliknutí detailu projektu](./images/v-01-00-22-projekty-sipka-projekt.png)
![Stránka detailu projektu - záložka Zakázky](./images/v-01-01-00-projekt-zakazky.png)

### Vytvoření zakázky

Vytvořit zakázku může uživatel s rolí *Globálního manažera* nebo *Manažera projektu* kliknutím na *Vytvořit zakázku* na stránce detailu projektu, v záložce *Zakázky*.

![Tlačítko vytvořit zakázku](./images/v-01-01-20-projekt-zakazky-sipka-vytvorit.png)
![Dialog pro vytvoření nové zakázky](./images/v-01-01-01-projekt-zakazka-modal-pridat.png)

### Úprava zakázky

Úpravu údajů zakázky může provést uživatel s rolí *Globálního manažera* nebo *Manažera projektu* kliknutím na ikonu upravit pro danou zakázku na stránce detailu projektu, v záložce *Zakázky*.

![Stránka detailu projektu - záložka Zakázky - tlačítko upravit](./images/v-01-01-21-projekt-zakazky-sipka-upravit.png)
![Dialog pro upravení zakázky](./images/v-01-01-02-projekt-zakazka-modal-upravit.png)

### Přidání manažera zakázky

Přidat manažera k zakázce může uživatel s rolí *Globálního manažera* nebo *Manažera projektu* kliknutím na *Přidat manažera* na stránce detailu projektu, v záložce *Manažeři zakázek*.

![Stránka detailu projektu - překliknutí záložky](./images/v-01-01-25-projekt-zakazky-sipka-zalozka-manazeri_zakazek.png)
![Stránka detailu projektu - záložka Manažeři zakázek](./images/v-01-03-20-projekt-manazeri_zakazek-sipka-vytvorit.png)
![Dialog pro přidaní manažera zakázky](./images/v-01-03-00-projekt-manazeri_zakazek-modal-pridat.png)

### Zobrazení detailu zakázky

Zobrazení detailu zakázky může uživatel s rolí *Globálního manažera* nebo *Manažera projektu* nebo *Manažera zakázky* rozkliknutím daného záznamu zakázky v tabulce na stránce detailu projektu, v záložce *zakázky*.

![Rozkliknutí detailu dané zakázky](./images/v-01-01-23-projekt-zakazky-sipka-detail.png)
![Stránka pro detail zakáky - záložka Výkazy](./images/v-02-00-01-zakazka-vykazy-seskupeni1.png)

#### Přehled výkazů v zakázce - změna seskupení

Přehled výkazů na stránce detailu zakázky lze seskupovat dvojím způsobem. Změna seskupení se provádí volbou v sekci filtování výsledku v přehledu.

![Stránka detailu dané zakázky - změna seskupení](./images/v-02-00-21-zakazka-vykazy-sipka-zmena_seskupeni.png)
![Stránka detailu dané zakázky - seskupení 2](./images/v-02-00-01-zakazka-vykazy-seskupeni2.png)

### Přidání zaměstnance do zakázky (vytvoření pracovní pozice zaměstnance)

Přidat zamstnance do zakázky, tzn. vytvořit mu v zakázce pracovní pozici, může uživatel s rolí *Globálního manažera* nebo *Manažera projektu* nebo *Manažera zakázky* kliknutím na *Přidat zaměstnanci pozici* na stránce detailu zakázky, v záložce *Zaměstnanci*.

![Rozkliknutí detailu dané zakázky](./images/v-01-01-23-projekt-zakazky-sipka-detail.png)
![Stránka detailu zakázky - překliknutí záložky](./images/v-02-00-20-zakazka-vykazy-sipka-zalozka_zamestnanci.png)
![Stránka detailu zakázky - záložka Zaměstnanci](./images/v-02-01-20-zakazka-zamestnanci-sipka-pridat.png)
![Dialog pro přidání pozice v zakázce](./images/v-02-01-01-zakazka-zamestnanci-modal-pridat.png)

### Úprava pozice zaměstnance v zakázce

Upravit zaměstnanci pracovní pozici v zakázce může uživatel s rolí *Globálního manažera* nebo *Manažera projektu* nebo *Manažera zakázky* kliknutím na ikonu upravit u dané pozice na stránce detailu zakázky, v záložce *Zaměstnanci*.

![Stránka detailu zakázky - záložka Zaměstnanci](./images/v-02-01-21-zakazka-zamestnanci-sipka-upravit.png)
![Dialog pro úpravu pocize v zakázce](./images/v-02-01-02-zakazka-zamestnanci-modal-upravit.png)

### Smazání pozice zaměstnance v zakázce

Upravit zaměstnanci pracovní pozici v zakázce může uživatel s rolí *Globálního manažera* nebo *Manažera projektu* nebo *Manažera zakázky* kliknutím na ikonu smazat u dané pozice na stránce detailu zakázky, v záložce *Zaměstnanci*.

![Stránka detailu zakázky - záložka Zaměstnanci](./images/v-02-01-22-zakazka-zamestnanci-sipka-smazat.png)

## Zaměstnanci

Modul Zaměstnanci slouží ke správě pracovních pozic zaměstnanců v jednotlivých zakázkách. U každé pozice lze evidovat období platnosti, rozsah úvazku a další údaje potřebné pro správné vytváření a vyhodnocování pracovních výkazů.

### Přehled pracovních pozic

Každý zaměstnanec může zorbrazit přehled svých pracovních pozic. Zobrazení pozic ostatních zaměstnanců mohou uživatelé s vyšším oprávněním než je role *Zaměstnanec* rozkliknutím záznamu v tabulce pro daného zaměstnance.

![Rozkliknutí detailu daného zaměstnance](./images/v-03-00-20-zamestnanec-sipka-detail.png)
![Stránka detailu zaměstnanec - překliknutí záložky](./images/v-03-01-91-zamestnanec-detail-vykazy-sipka-pozice.png)

### Vytvoření pracovní pozice

Vytvořit pracovní pozici zaměstnanci mohou uživatelé s vyšším oprávněním než je role *Zaměstnanec*.

Vytvořit pozici lze dvěma způsoby:
1) Přes zaměstnance
Otevření přehledu pracovních pozic daného zaměstnance viz *Zaměstnanci > Přehled pracovních pozic*
![Stránka detailu zaměstnance - záložka Pozice](./images/v-03-02-91-zamestnanec-detail-pozice-sipka-pridat.png)
![Dialog pro přidání pracovní pozice zaměstnanci](./images/v-03-02-01-zamestnanec-detail-pozice-modal-pridat.png)

2) Přes zakázku – viz *Zakázky > Přidání zaměstnance do zakázky (vytvoření pracovní pozice zaměstnance)*

### Úprava pracovní pozice

Upravit pracovní pozici zaměstnanci mohou uživatelé s vyšším oprávněním než je role *Zaměstnanec*.

Vytvořit pozici lze dvěma způsoby:
1) Přes zaměstnance
![Stránka detailu zaměstnane - záložka Pozice](./images/v-03-02-92-zamestnanec-detail-pozice-sipka-upravit.png)
![Dialog pro úpravu pocize v zakázce](./images/v-03-02-02-zamestnanec-detail-pozice-modal-upravit.png)

2) Přes zakázku – viz *Zakázky > Úprava pozice zaměstnance v zakázce*

## Docházka

Modul Docházka slouží k nahrání docházkových dat exportovaných ze systému IMIS. Nahraná data představují podklad pro vytváření pracovních výkazů a kontrolu vykázané pracovní doby.

### Export docházky z IMIS

![Export docházky z IMIS - krok 1](./images/v-imis-01.png)
![Export docházky z IMIS - krok 2](./images/v-imis-02.png)
![Export docházky z IMIS - krok 3](./images/v-imis-03.png)

Příklad exportu docházky za měsíc duben 2026. Pokud není datum vylněn, exportuje se automaticky aktuální měsíc. Datum pro export měsíční docházky je potřeba zadat vždy od 1. daného měsíce do 1. následujícího měsíce, nikoliv od 1. do 31. téhož měsíce.

### Nahrání docházky z IMIS

V současné době není aplikace propojena se systémem IMIS. Docházku je proto nutné nejprve exportovat ze systému IMIS a následně ji nahrát do aplikace Výkazy.

![Stránka Moje Výkazy / Detail zaměstnance](./images/v-03-01-22-zamestnanec-sipka-nahrat.png)
![Dialog pro nahrání pracovní docházky](./images/v-03-01-01-zamestnanec-detail-vykazy-modal-nahrat_dochazku.png)

## Výkazy

Modul Výkazy slouží k evidenci, úpravám a schvalování pracovních výkazů zaměstnanců. Uživatel zde může sledovat stav jednotlivých výkazů, doplňovat vykázanou práci a pracovat s připomínkami vzniklými během schvalovacího procesu.

### Zobrazení výkazu

1) Přes zaměstnance

![Stránka Zaměstnanci - zobrazení stránky detailu daného zaměstnance](./images/v-04-00-20-zamestnaneci-zamestnanec-sipka-zamestnanec.png)
![Stránka detailu zaměstnance - záložka Výkazy - zobrazení kombinovaného výkazu pro daný měsíc](./images/v-04-00-21-zamestnanec-vykazy-sipka-detail_mesice.png)
![Stránka pro kombinovaný výkaz](./images/v-04-01-00-vykaz-prehled.png)

2) Přes zakázku – tento krok vychází ze zobrazení detailu zakázky (viz *Zakázky > Zobrazení detailu zakázky*)

    2.1 Seskupení dle měsíce a zaměstnance
    ![Zobrazení stránky pro kombinovaný výkaz přes sekupení 1](./images/v-02-00-22-zakazka-vykazy-sipka-zobrazit_vykaz_seskupeni1.png)

    2.2 Seskupení dle zaměstnance a měsíce
    ![Zobrazení stránky pro kombinovaný výkaz přes sekupení 2](./images/v-02-00-23-zakazka-vykazy-sipka-zobrazit_vykaz_seskupeni2.png)


### Zvětšení tabulky s kombinovaným výkazem

![Stránka pro kombinovaný výkaz - tlačítko zvětšit](./images/v-04-01-20-vykaz-prehled-sipka-zvetsit.png)

### Úprava výkazu

Níže na obrázku jsou popsány základní prvky a operace kombinovaného výkazu:

1) Sloupec pro projektovou činnost – po najetí myší na název sloupce se zobrazí detailní informace (projekt, zakázka, úvazek)

2) Zamykání buněk tabulky – ikona zámečku slouží pro uzamčení hodnoty buňky v tabulce. Takto zamčené buňky si při generování hodnot uchovají zadanou fixní hodnotu.

3) Zkopírování odpracovaných hodin v projektu – kliknutím na ikonu kopírovat se zkopírují hodnoty celého sloupce (pro předenesní hodnot například do dokumentu pro výkaz projektové činnosti).

4) Generování hodnot pro celý výkaz - kliknutím na ikonu generovat se vygenerují hodnoty pro celou tabulku výkazu.

5) Generování hodnot pro jednotlivý řádek - kliknutím na ikonu generovat se vygenerují hodnoty pro daný řádek.

6) Kontrolní součtový řádek – ve formátu *celkem vyplněných hodin / celkem hodin potřebných vyplnit*. Sloupce projektových činností je potřeba splnit přesně, u sloupce pro kmenový úvazek je povolen mírný přesah odpracovaných hodin (max 2 hod).

![Kombinovaný výkaz - popis](./images/v-04-01-20-vykaz_zoom-sipka-popis.png)

#### Chybové hlášky

Chybové hlášky jsou dvojí závažnosti:
- červené – výstražné,
- žluté – upozorňující.

S červenými hláškami nelze výkaz odeslat ke schváleni, s žlutými je odeslání již možné. Chybové hlášky mohou být pro konkrétní buňky nebo pro celé řádky výkazu.

### Uložení změn v kombinovaném výkazu

Po provedení úprav je vždy zapotřebí uloži změny kliknutím na tlačítko *Uložit změny*.

![Stránka pro kombinovaný výkaz - tlačítko Uložit změny](./images/v-04-01-22-vykaz-prehled-sipka-ulozit_zmeny.png)

### Schválování výkazu

#### Odeslání kombinovaného výkazu ke schválení

Pokud je vyplněný kombinovaný výkaz bez červených chybových hlášek, lze jej odeslat ke schválení kliknutím na tlačítko *Odeslat ke schválení*. Po odeslání jsou všechny části kombinovaného výkazu ve stavu *Ke schválení*. Provádí zaměstnanec, jehož se pracovní výkaz týka.

![Stránka pro kombinovaný výkaz - tlačítko Odeslat ke schválení](./images/v-04-01-21-vykaz-prehled-sipka-odeslat_ke_schvalenit.png)
![Dialog pro odeslání výkazu ke schválení](./images/v-04-00-03-vykaz-modal-odeslat_ke%20schvaleni.png)

#### Vrácení kombinovaného výkazu k přepracování

*Globální manažer*, *Manažer projektu* či *Manažer zakázky* může vrátit celý kombinovaný výkaz k přepracování kliknutím na tlačítko *Vrátit k přepracování*. Po této akci jsou všechny části kombinovaného výkazu ve stavu *Rozpracovaný*. 

![Stránka pro kombinovaný výkaz - tlačítko Vrátit k přepracování](./images/v-04-02-21-vykaz-ke_schvaleni-sipka-vratit.png)
![Dialog pro vrácení celého výkazu k přepracování](./images/v-04-00-07-vykaz-modal-vratit_cely.png)

#### Schválení projektové činnosti

Pokud je stav projektové činnosti ve stavu *Ke schválení* může *Manažer projektu* či *Manažer zakázky* schválit projektovou část kombinovaného výkazu kliknutím na tlačítko *Schválit* u dané projektové činnosti. Po této akce je projektová část ve stavu *Schváleno*.

![Stránka pro kombinovaný výkaz - tlačítko Schválit u projektové činnosti](./images/v-04-02-20-vykaz-ke_schvaleni-sipka-schvalit-projekt.png)
![Dialog pro schválení projektové činnosti](./images/v-04-00-04-vykaz-modal-schvalit_projekt.png)

#### Vrácení projektové činnosti k přepracování

*Manažer projektu* či *Manažer zakázky* může vrátit projektovou část kombinovaného výkazu kliknutím na tlačítko *Vrátit* u dané projektové činnosti.

![Stránka pro kombinovaný výkaz - tlačítko Vrátit u projektové činnosti](./images/v-04-02-20-vykaz-ke_schvaleni-02-sipka-vratit_projekt.png)
![Dialog pro vrácení projektové činnosti k přepracování](./images/v-04-00-05-vykaz-modal-vratit_projekt.png)

#### Schválení kombimovaného výkazu

Pokud jsou všechny části kombinovaného výkazu ve stavu *Ke schválení* může poté zaměstnanec (jehož se pracovní výkaz týká) nebo *Globální manažer* provést schválení celého výkazu kliknutím na tlačítko *Schválit*. Po této akci jsou všechy části kombinovaného výkazu ve stavu *Schváleno* a výkaz je uzamčený úpravám.

![Stránka pro kombinovaný výkaz - tlačítko Schválit](./images/v-04-02-20-vykaz-ke_schvaleni-03-sipka-schvalit.png)
![Dialog pro schválení celého kombinovaného výkazu](./images/v-04-00-06-vykaz-modal-schvalit_cely.png)

#### Odemknutí kombinovaného výkazu

Odemknout zpět schválený výkaz může zaměstnanec nebo *Globální manažer* kliknutím na talčítko *Odemknout*. Po této akci jsou všechny části kombinovaného výkazu ve stavu *Rozpracováno* a je možná jejich editace.

![Stránka pro kombinovaný výkaz - tlačítko Odemknout](./images/v-04-02-20-vykaz-ke_schvaleni-04-sipka-odemknout.png)
![Dialog pro odemčení celého kombinovanýho výkazu](./images/v-04-00-08-vykaz-modal-odemknout.png)

### Historie schvalování a komentářová sekce

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

**Lze nahrát docházku naráz za více měsíců?**

Nahrát docházku za více měsíců lze pouze formou "jeden měsíc = jeden soubor". Dialog pro nahrání docházky umožňuje přiložit více souborů.


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

# Scenario Cases
TODO: validní hodnoty ID-projektu, ID-zakázky, Kód pozice


**Projekty**

Scenario: Zobrazení stránky 'Projekty'
    Given Jsem přihlášený uživatel
    And Jsem na libovolné stránce aplikace
    When V navigační liště kliknu na položku 'Projekty'
    Then Vídím stránku 'Projekty'
    And Vidím seznam karet projektů (může být prázdný)

Scenario: Vyhledání projektu podle názvu
    Given Existuje projekt 'Test projekt' s id 'PROJ-1234-5678-111'
    And Jsem na stránce 'Projekty'
    When Zadám do pole 'Hledat' název projektu 'Test projekt'
    Then Vidím ve výsledcích vyhledávání projekt 'Test projekt' s id 'PROJ-1234-5678-111'

Scenario: Vyhledání projektu podle id
    Given Existuje projekt 'Test projekt' s id 'PROJ-1234-5678-111'
    And Jsem na stránce 'Projekty'
    When Zadám do pole 'Hledat' id projektu 'PROJ-1234-5678-111'
    Then Vidím ve výsledcích vyhledávání projekt 'Test projekt' s id 'PROJ-1234-5678-111'

Scenario: Vytvoření nového projektu
    Given Jsem na stránce 'Projekty'
    When Kliknu na tlačítko 'Přidat projekt'
    And Vyplním pole 'Název projektu' hodnotou 'Test projekt'
    And Vyplním pole 'ID projektu' hodnotou '1234 5678 111'
    And Vyplním pole 'Datum začátku' hodnotou '1.1.2026'
    And Potvrdím formulář
    Then V přehledeu projektů vidím kartu s názvem 'Test projekt'

Scenario: Zobrazení detailu projektu
    Given Jsem na stránce 'Projekty'
    And Existuje projekt 'Test projekt'
    When Kliknu na kartu projektu s názevem 'Test projekt'
    Then Vídím stránku s detailem projektu 'Test projekt'

Scenario: Upravení údajů projektu
    Given
    When
    Then

Scenario: Odstranění projektu
    Given
    When
    Then


**Zakázky**

Scenario: Vytvoření nové zakázky v projektu
    Given Jsem na stránce detailu projektu 'Test projekt'
    And Jsem v záložce 'Zakázky'
    When Kliknu na tlačítko 'Přidat zakázku'
    And Vyplním pole 'ID zakázky' hodnotou '1234 5678 222'
    And Vyplním pole 'Název zakázky' hodnotou 'Test zakázka'
    And Potvrdím formulář
    Then V tabulce zakázek vídím záznam pro 'Test zakázka'

Scenario: Vyhledání zakázky podle názvu
    Given Existuje projekt 'Test projekt'
    And Existuje zakázka 'Test zakázka' s id 'CONT-1234-5678-111'
    And Jsem na stránce detailu projektu 'Test projekt'
    When Zadám do pole 'Hledat' název zakázky 'Test zakázka'
    Then Vidím ve výsledcích vyhledávání zakázku 'Test zakázka' s id 'CONT-1234-5678-111'

Scenario: Vyhledání zakázky podle id
    Given Existuje projekt 'Test projekt'
    And Existuje zakázka 'Test zakázka' s id 'CONT-1234-5678-111'
    And Jsem na stránce detailu projektu 'Test projekt'
    When Zadám do pole 'Hledat' id zakázky 'CONT-1234-5678-111'
    Then Vidím ve výsledcích vyhledávání zakázku 'Test zakázka' s id 'CONT-1234-5678-111'

Scenario: Upravení údajů zakázky
    Given
    When
    Then

Scenario: Odstranění zakázky
    Given
    When
    Then

Scenario: Přidání manažera k zakázce
    Given Existuje projekt 'Test projekt'
    And Existuje zakázka 'Test zakázka'
    And Existuje zaměstnanec 'Jan Novák'
    And Jsem na stránce detailu projektu 'Test projekt'
    And Jsem v záložce 'Manažeři zakázek'
    When Kliknu na tlačítko 'Přidat manažera'
    And Zvolím v poli 'Zakázka' hodnotu 'Test zakázka'
    And Zvolím v poli 'Zaměstnanec' hodnotu 'Jan Novák'
    And Potvrdím formulář
    Then V tabulce zakázek vídím záznam se zaměstnanecm 'Jan Novák'

Scenario: Odebrání mažera ze zakázky
    Given
    When
    Then

Scenario: Přidání pozice zaměstnanci přes detail zakázky
    Given Existuje zakázka 'Test zakázka'
    And Existuje zaměstnanac 'Jan Novák'
    And Jsem na stránce detailu zakázky 'Test zakázka'
    And Jsem v záložce Zaměstnanci
    When Kliknu na tlačítko 'Přidat zaměstnanci pozici'
    And Zvolím v poli 'Zaměstnanec' hodnotu 'Jan Novák'
    And Vyplním pole 'Kód pozice' hodnotou '111'
    And Vyplním pole 'Název pozice' hodnotou 'Analytik'
    And Vyplním pole 'Úvazek' hodnotou '30'
    And Vyplním pole 'Datum začátku' hodnotou '1.3.2026'
    Then V tabulce zakázek vídím záznam se zaměstnanecm 'Jan Novák' a zadanými údaji

Scenario: Zobrezení přehledu výkazů v zakázce
    Given
    When
    Then


**Zaměstnanci**

Scenario: Zobrazení stránky 'Zaměstnanci'
    Given Jsem přihlášený uživatel
    And Jsem na libovolné stránce aplikace
    When V navigační liště kliknu na položku 'Zaměstnanci'
    Then Vídím stránku 'Zaměstnanci'
    And Vidím seznam zaměstnanců (může být prázdný)

Scenario: Vyhledání zaměstnanace podle jména
    Given Existuje zaměstnanec 'Jan Novák' s osobním číslem '1001'
    And Jsem na stránce 'Zaměstnanci'
    When Zadám do pole 'Hledat' jméno zaměstnance 'Jan Novák'
    Then Vidím ve výsledcích vyhledávání zaměstnance 'Jan Novák' s osobním číslem '1001'

Scenario: Vyhledání zaměstnanace podle osobního čísla
    Given Existuje zaměstnanec 'Jan Novák' s osobním číslem '1001'
    And Jsem na stránce 'Zaměstnanci'
    When Zadám do pole 'Hledat' osobní číslo zaměstnance '1001'
    Then Vidím ve výsledcích vyhledávání zaměstnance 'Jan Novák' s osobním číslem '1001'

Scenario: Zobrazení detailu zaměstnance 'Jan Novák'
    Given Existuje zaměstnanec 'Jan Novák'
    And Jsem na stránce 'Zaměstnanci'
    When V tabulce zaměstnanců kliknu na záznam s údaji 'Jan Novák'
    Then Vidím stránku detail zaměstnance 'Jan Novák'

Scenario: Přidání pozice zaměstnanci přes detail zaměstnance
    Given Existuje zakázka 'Test zakázka'
    And Existuje zaměstnanac 'Jan Novák'
    And Jsem na stránce detailu zaměstnance 'Jan Novák'
    And Jsem v záložce 'Pozice'
    When Kliknu na tlačítko 'Přidat pozici'
    And Zvolím v poli 'Projek' hodnotu 'Test projekt'
    And Zvolím v poli 'Zakázka' hodnotu 'Test zakázka'
    And Vyplním pole 'Kód pozice' hodnotou '111'
    And Vyplním pole 'Název pozice' hodnotou 'Analytik'
    And Vyplním pole 'Úvazek' hodnotou '30'
    And Vyplním pole 'Datum začátku' hodnotou '1.3.2026'
    Then V tabulce pozic vídím záznam se zaměstnanecm 'Jan Novák' a zadanými údaji
    Then

Scenario: Odebrání pozice zaměstnanci
    Given
    When
    Then

Scenario: Zobrezení přehledu výkazů zaměstnance
    Given
    When
    Then


**Výkazy**

Scenario: 
    Given
    When
    Then

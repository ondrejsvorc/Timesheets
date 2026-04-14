Feature: Zaměstnanci

Background:
    Given Jsem přihlášený uživatel
    And Jsem na libovolné stránce aplikace

Scenario: Zobrazení stránky 'Zaměstnanci'
    When V navigační liště kliknu na položku 'Zaměstnanci'
    Then Vidím stránku 'Zaměstnanci'
    And Vidím seznam zaměstnanců (může být prázdný)

Scenario: Vyhledání zaměstnance podle jména
    Given Existuje zaměstnanec 'Jan Novák' s osobním číslem '1001'
    And Jsem na stránce 'Zaměstnanci'
    When Zadám do pole 'Hledat' jméno zaměstnance 'Jan Novák'
    Then Vidím ve výsledcích vyhledávání zaměstnance 'Jan Novák' s osobním číslem '1001'

Scenario: Vyhledání zaměstnance podle osobního čísla
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
    And Existuje zaměstnanec 'Jan Novák'
    And Jsem na stránce detailu zaměstnance 'Jan Novák'
    And Jsem v záložce 'Pozice'
    When Kliknu na tlačítko 'Přidat pozici'
    And Zvolím v poli 'Projekt' hodnotu 'Test projekt'
    And Zvolím v poli 'Zakázka' hodnotu 'Test zakázka'
    And Vyplním pole 'Kód pozice' hodnotou '111'
    And Vyplním pole 'Název pozice' hodnotou 'Analytik'
    And Vyplním pole 'Úvazek' hodnotou '30'
    And Vyplním pole 'Datum začátku' hodnotou '1.3.2026'
    Then V tabulce pozic vidím záznam se zaměstnancem 'Jan Novák' a zadanými údaji

Scenario: Odebrání pozice zaměstnanci
    Given
    When
    Then
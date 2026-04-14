Feature: Zakázky

Background:
    Given Jsem přihlášený uživatel

Scenario: Vytvoření nové zakázky v projektu
    Given Jsem na stránce detailu projektu 'Test projekt'
    And Jsem v záložce 'Zakázky'
    When Kliknu na tlačítko 'Přidat zakázku'
    And Vyplním pole 'ID zakázky' hodnotou '1234 5678 222'
    And Vyplním pole 'Název zakázky' hodnotou 'Test zakázka'
    And Potvrdím formulář
    Then V tabulce zakázek vidím záznam pro 'Test zakázka'

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
    Then V tabulce zakázek vidím záznam se zaměstnancem 'Jan Novák'

Scenario: Odebrání manažera ze zakázky
    Given
    When
    Then

Scenario: Přidání pozice zaměstnanci přes detail zakázky
    Given Existuje zakázka 'Test zakázka'
    And Existuje zaměstnanec 'Jan Novák'
    And Jsem na stránce detailu zakázky 'Test zakázka'
    And Jsem v záložce Zaměstnanci
    When Kliknu na tlačítko 'Přidat zaměstnanci pozici'
    And Zvolím v poli 'Zaměstnanec' hodnotu 'Jan Novák'
    And Vyplním pole 'Kód pozice' hodnotou '111'
    And Vyplním pole 'Název pozice' hodnotou 'Analytik'
    And Vyplním pole 'Úvazek' hodnotou '30'
    And Vyplním pole 'Datum začátku' hodnotou '1.3.2026'
    Then V tabulce zakázek vidím záznam se zaměstnancem 'Jan Novák' a zadanými údaji
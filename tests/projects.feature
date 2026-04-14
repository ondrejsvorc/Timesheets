Feature: Projekty

Background:
    Given Jsem přihlášený uživatel
    And Jsem na libovolné stránce aplikace

Scenario: Zobrazení stránky 'Projekty'
    When V navigační liště kliknu na položku 'Projekty'
    Then Vidím stránku 'Projekty'
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
    Then V přehledu projektů vidím kartu s názvem 'Test projekt'

Scenario: Zobrazení detailu projektu
    Given Jsem na stránce 'Projekty'
    And Existuje projekt 'Test projekt'
    When Kliknu na kartu projektu s názvem 'Test projekt'
    Then Vidím stránku s detailem projektu 'Test projekt'

Scenario: Upravení údajů projektu
    Given
    When
    Then

Scenario: Odstranění projektu
    Given
    When
    Then
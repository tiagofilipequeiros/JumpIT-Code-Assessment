Feature: Stock management
  Anyone signed in can add and remove stock.
  Stock can never go below zero, even when many people act at the same time.

  Background:
    Given I am signed in as "Sam User"

  Scenario: Removing stock that is available
    Given the product "Microscope Objective 10x" has 42 in stock
    When I remove 5 from the stock of "Microscope Objective 10x"
    Then the request succeeds
    And the product "Microscope Objective 10x" has 37 in stock

  Scenario: Adding stock
    Given the product "LED Illuminator" has 0 in stock
    When I add 20 to the stock of "LED Illuminator"
    Then the request succeeds
    And the product "LED Illuminator" has 20 in stock

  Scenario: Removing more stock than is available
    Given the product "Microscope Objective 100x Oil" has 3 in stock
    When I remove 4 from the stock of "Microscope Objective 100x Oil"
    Then the request is rejected with "InsufficientStock"
    And the product "Microscope Objective 100x Oil" has 3 in stock

  Scenario: Many people take the last items at the same time
    Given the product "Microscope Objective 100x Oil" has 3 in stock
    When 10 people each remove 1 from the stock of "Microscope Objective 100x Oil" at the same time
    Then 3 requests succeed and 7 are rejected with "InsufficientStock"
    And the product "Microscope Objective 100x Oil" has 0 in stock

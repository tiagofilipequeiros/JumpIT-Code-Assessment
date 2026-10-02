Feature: Product editing
  Editors and admins manage the catalogue. Normal users can only change stock.

  Scenario: Creating a product gives it a new 6-digit id
    Given I am signed in as "Erin Editor"
    When I create a product "Polarizing Filter" priced 45.50 with 2 in stock in "Accessories"
    Then the request succeeds
    And the new product has a 6-digit id
    And searching for "polar" finds "Polarizing Filter"

  Scenario: Normal users cannot create products
    Given I am signed in as "Sam User"
    When I create a product "Polarizing Filter" priced 45.50 with 2 in stock in "Accessories"
    Then the request is rejected with "Forbidden"

  Scenario: Invalid products are rejected
    Given I am signed in as "Erin Editor"
    When I create a product "X" priced -1 with 2 in stock in "Accessories"
    Then the request is rejected with "ValidationFailed"

  Scenario: Two people edit the same product at the same time
    Given "Erin Editor" and "Alex Admin" both open the product "Eyepiece 10x"
    When "Erin Editor" saves the price 85.00
    And "Alex Admin" saves the price 90.00
    Then the last request is rejected with "ConcurrencyConflict"
    And the product "Eyepiece 10x" costs 85.00

  Scenario: Only admins can delete products
    Given I am signed in as "Erin Editor"
    When I delete the product "Eyepiece 10x"
    Then the request is rejected with "Forbidden"

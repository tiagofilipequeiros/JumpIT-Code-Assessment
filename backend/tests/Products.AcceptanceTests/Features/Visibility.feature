Feature: Visibility
  Disabling hides products from normal users without losing them.
  Deleting a category keeps its products in Uncategorized, where only editors and admins see them.

  Scenario: A disabled product is hidden from normal users
    Given I am signed in as "Erin Editor"
    When I disable the product "Lens Cleaning Kit"
    Then "Sam User" does not see "Lens Cleaning Kit"
    But "Erin Editor" sees "Lens Cleaning Kit" when showing hidden products

  Scenario: Re-enabling a product makes it visible again
    Given I am signed in as "Erin Editor"
    And I disable the product "Lens Cleaning Kit"
    When I enable the product "Lens Cleaning Kit"
    Then "Sam User" sees "Lens Cleaning Kit"

  Scenario: Deleting a category keeps its products
    Given I am signed in as "Alex Admin"
    When I delete the category "Eyepieces"
    Then the request succeeds
    And the product "Eyepiece 10x" is in the category "Uncategorized"
    And "Sam User" does not see "Eyepiece 10x"

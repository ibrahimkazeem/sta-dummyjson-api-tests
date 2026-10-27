Feature: DummyJSON API coding challenge
  Validate the nine requested behaviours without assuming simulated writes persist.

  @auth
  Scenario: Authenticate a user and store the access token
    When I authenticate with the documented demo credentials
    Then an access token is stored for this scenario

  @auth
  Scenario: Retrieve the current authenticated user
    Given I authenticate with the documented demo credentials
    When I request the current authenticated user
    Then the user is 1 named "Emily" "Johnson"

  @users
  Scenario: Count all users with red hair
    When I filter users with red hair
    Then the count and identities match the complete user dataset

  @users
  Scenario: Add a user with hair and full address details
    When I add a user with all required nested details
    Then the created response contains all submitted user details

  @products
  Scenario: Validate the price of iPhone 13 Pro
    When I search for "iPhone 13 Pro"
    Then the exact matching product costs 1099.99

  @products
  Scenario: Retrieve only the fifteenth product using pagination
    When I request one product after skipping fourteen products
    Then only product 15 titled "Wooden Bathroom Sink With Mirror" priced 799.99 is returned

  @products
  Scenario: Update the title of product 121
    When I update product 121 title to "QA Updated Product Title"
    Then product 121 has title "QA Updated Product Title" in the update response

  @carts
  Scenario: Add five products and validate discount percentages
    Given five distinct catalog products with mixed quantities
    When I add those products to a cart
    Then the product discount percentages and their sum match the catalog
    And the effective cart discount percentage agrees with the monetary totals

  @carts
  Scenario: Delete an existing cart
    When I delete existing cart 1
    Then cart 1 is marked deleted with a valid timestamp

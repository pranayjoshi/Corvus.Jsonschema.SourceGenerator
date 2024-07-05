# Input Attributes for the Source Generator

* Status: Accepted
* Deciders: Greg Dennis, Matthew Adams
* Date: 2 July 2024

## Context and Problem Statement

Decide between Partial level and Assembly level attributes as a medium of user input for our source generators

## Decision Drivers

* Easy to use
* Flexibility

## Considered Options

### Assembly Level Attribute

* Good, because Scalable
* Bad, because terminal like input architecture
* Bad, because not too much flexible with the usage.

### Partial Level Attribute

* Good, because easy to use and code friendly input architecture
* Good, because flexible with the usage
* Bad, because might feel complicated while importing lots of packages.

## Decision Outcome

"Partial level Attribute" was chosen because it provides an easy to use and user friendly input architecture compared to Assembly level attribute which resembles terminal style input architecture. It also avoids writing some extra lines while generating the schema.
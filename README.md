## Project Description
My ant bot for Google AI Challenge 2011 goes open source here. (The code will be released soon after challenge ended)

I never studied computer science nor algorithms, but the challenge made me to. I tried my best to write better AI in the limited time I had. Wow this challenge is addictive.

v3 of my bot [ranked 569 in finals](http://ants.aichallenge.org/profile.php?user=645) and v1 of it [ranked 943](http://ants.aichallenge.org/profile.php?user=14102).

My bot uses these strategies.

### Ant spawning:
Uses new born out of hill solution, to not raze my hills.


![](https://raw.githubusercontent.com/salarcode/SalarAnts/master/Documents/AntSpawning.png)


### Path finding & Map Exploring:
Uses A* heavily modified code, which has limited distance for search in case goal is far and cached results to prevent timeouts.

This shows how far (y axis) can ant search go depending on my ants count (x axis).


![](https://raw.githubusercontent.com/salarcode/SalarAnts/master/Documents/AStarSearchLimitPow.png)


An example of food collecting using A*

![](https://raw.githubusercontent.com/salarcode/SalarAnts/master/Documents/FoodCollecting.png)

### Defense strategy:
Uses two approach, when best solution is to evade, it first try to evade from group of ant power, if that approach failed, it tries to run away from closest enemy.

![](https://github.com/salarcode/SalarAnts/raw/master/Documents/EvadingAnt.png)

### Attack strategy:
Mainly based on focus of my ants on enemy, tries to get the ant closer to enemy, then attacks if there is enough ant to attack. Before attack it tries to simulate the situation which ant will die if or not. If dies are more than kills attack doesn't happen. It's kind of sacrifice my ants does. It's aggressive.


![](https://raw.githubusercontent.com/salarcode/SalarAnts/master/Documents/AttackPressure.png)


Attack strategy has lots of issues, mainly unable to predict enemy moves, which leads my ants to be defeated.

I also have written an attack strategy based on matrix and influenced map, it is incomplete and not used but it has very good performance, wish I had more time to study the concept before deadline :)

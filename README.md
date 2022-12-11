 # DRL-RNN-LSTM-BOX-SIM
- ## DRL - Deep Reinforcement Learning
- ## RNN - Recurrent Neural Network
- ## LSTM - Long Short Term Memory (RNN derivative)
- ## BOX - Box. Box. Box. (RL Agent's existence)
- ## SIM - Simulation in Unity/Gym (RL Environment)

# Project Members: Bryan Boyett & Yueqi Peng & ??DRL_Teammate_#3!??

## DRL
![](images/Screenshot%20from%202022-12-03%2023-56-10.png)

## LSTM / RNN evo
### Pointer Networks https://arxiv.org/pdf/1506.03134.pdf
- ### **Read paper and discover why authors recommended Pointer Networks paper**
  - #### Pointer Networks are useful for solving combinatorial optimization problems (box placement is combinatorial), allowing to train and infer on different sized inputs (sometimes there may be 505 boxes, sometimes 347, based on size of boxes (less larger boxes or more smaller boxes))
  - #### "previous attention attempts in that, instead of using attention to blend hidden units of an encoder to a context vector at each decoder step, it uses attention as a pointer to select a member of the input sequence as the output. We call this architecture a Pointer Net (Ptr-Net)" 
  - #### [ Pointer-Network ] "uses attention as a pointer to select a member of the input sequence as the output" ... instead of [ Attention-based RNN ] "using attention to blend hidden units of an encoder to a context vector at each decoder step" 
  - #### Pointer Network
  ![](images/Screenshot%20from%202022-12-06%2015-45-25.png) 
  vs
  - #### Attention-based RNN
  ![](images/Screenshot%20from%202022-12-06%2015-42-33.png) 
  - #### MVP Evolve: compare two agents in same environment and plot performance with plotly, to show performance difference in Attention-based RNN vs. Self-Attention Transformer policies. 
    - #### MVP Evolve: after the two agents, could also throw additional agents with different models in the environment, such as hueristics, non-seq2seq models, like RL models ppo etc.
- ### Sequence-to-Sequence (Seq2Seq) which have been applied to language translation models can also be applied to our fitting boxes model. 
- ### Seq2Seq & Greedy https://youtu.be/wzfWHP6SXxY?t=2474
  - #### @ 41:11 putting a word in a wrong place would cause future words to be put in the wrong (non-optimal) place
  - #### @ 41:11 putting a box in a wrong place would cause future words to be put in the wrong (non-optimal) place
- ### non-Greedy algorithms > Greedy algorithms 
- ### May be able to use Decision Transformers (RL Transformer) as it approaches the problem with the reward hypothesis in reinforcement learning (the traditional RL reward hypothesis is a greedy hypothesis that says maximize the reward, where instead may want to consider optimality as desired reward or desired reward state) https://huggingface.co/docs/transformers/model_doc/decision_transformer

## Attention ~ Probabilistic
- ### Attention-mechanism is a Probabilistic-mechanism

## SIM / BOX
### Unity ML-Agents vs. Gym Dilemma PyTorch vs. JAX
- #### Unity ML-Agents would require choosing PyTorch over JAX for custom training
  - #### JAX is "recommended!" also new cool kid on the block (Tensorflow's redeemer)
  - #### Want to stick with JAX and be humble and listen to smarter people's leading recommendations!
  - #### We have a non-complex enough environment that can be modeled without Unity, so **choosing JAX > Unity & PyTorch**
### Bin packing problem > Cutting stock problem > Knapsack problem
  - #### subsets of combinatorial optimization problems
  - #### NP-hard, NP-complete if agent has to decide if all boxes can fit in bin
  - #### Policy should determine

## Box
- ### Agent will sample spatial information of Box
  - ### size, position, mass
- ### Mass: Structural Integrity to make sure Boxes aren't crushed (w = mg)
  - #### rl environment has physics engire

## RL environment/engine
- ### Unity (C#) vs. Gym (python)
- ### Unity ml-agents vs. Acme, a DeepMind RL library for modelling agents behavior
  - #### https://github.com/deepmind/acme
  - #### https://www.deepmind.com/publications/acme-a-new-framework-for-distributed-reinforcement-learning

## plotly
- ### showcase the evolution and dynamics of the agent's policy
- ### because the optimal answer may not be known beforehand by the human, (which is a benefit of DRL as the human can learn the unknown from the agent), we want to use graphs to show how the agent approached local optima and global optima

![](images/Screenshot%20from%202022-12-04%2022-44-53.png)
- ### perhaps only terminal action needs to be remembered by NN


<br/>
<br/>
<br/>


# Phase 1: Exploratory Data Understanding and Modelling
- ### Business Objective: reduce wasting space in 3d packing
- ### ML Objective: use Reinforcement Learning to solve variable-size, permutation-invariant combinatorial problems


## Data: State generated through RL environment
### <ins>Constant: Data that’s provided</ins>
- 3d size of the container, 3d size of the boxes, quantity of boxes, id of each box 
- Position of the truck
- Gravity and mass of boxes

### <ins>Dynamic: Data that the RL agent can change</ins>
- State: data observed by the RL agent, input to the agent brain’s policy
  - State tensor: [[[x1, y1, z1], [x2, y2, z2], etc.], [[l1, w1, h1], [l2, w2, h2], etc], [m1, m2, etc]]
    - 1st dimension describes the 3d position of the boxes
    - 2nd dimension describes the size of the boxes
    - 3rd dimension describes the mass of the boxes
- Action: data changed by the RL agent, output from the agent brain’s policy
  - Action-space tensor: [[0, 1], [+1x, -1x, +1y, -1y, +1z, -1z]]
    - This 2d tensor consists of 2 dimensions of action
    - 1st dimension is a discrete action space that describes a boolean whether a box is picked up or dropped
    - 2nd dimension is a discrete action space that describes 6 directions the agent can take


## Model: Policy generated through DL(+RL) patterns
<ins>Deep Learning: Agent brain’s policy</ins>
- Learning options: 
  - Heuristics
  - LSTM
  - Pointer Network
  - Attention-based RNN
  - Transformer
Attention mechanisms are probabilistic mechanisms

<ins>Reinforcement Learning: Agent’s reward</ins>
Reward options: maximizing reward (traditional reward hypothesis) vs the desired reward
- Penalty for crushing boxes
  - Need to ensure structural integrity of boxes, using their mass and the gravity of the reinforcement learning agent's environment simulated physics can model the stress especially of the boxes on the bottom
  - some of the agents box placements may possibly recommend a bridge like support placement, where the bottom face of the box is supported at the edges by other boxes acting like pillars. If so, then like a bridge need to ensure box will not collapse in the center.
- Reward for more boxes placed
  - MVP reward schedule is fairly simple (+1 for each additional box placed in container), future Evolved MVP reward schedule could possible introduce additional dimensions (speed of packing?)
- Generalize agent to more general environments, such as changed 3D dimension of the container if say instead of packing for ground transportation (truck) but for air transportation (airplane) 
  - https://en.wikipedia.org/wiki/Unit_load_device#Types
  - ![](https://upload.wikimedia.org/wikipedia/commons/8/81/Unit_load_device_sizes.png)

Environment and Tools
- RL Environment options: (**Environment-driven AI!**)
  - Unity
  - Gym
  - dm_env
- RL Agents framework:
  - Acme

## Intution/Heuristics: the most basic idea behind heuristics of bin packing 
(although we are using NN instead of heuristics, there is intuition from heuristics)
1. It is a NP hard problem, the motivation of all heuristics is thus to reduce the complexity in linear programming
2. When more boxes are of the same size, the complexity decreases
3. heuristics ideas: first pack large items (reduce number of combinations), group items into size group and round (reduce number of contraints in LP), 
use fractional LP (relaxes constraints), add back the smaller items at the very end
the deep mind's paper on AI winning at Atari shows clearly that they developed an expert level strategy, and right now, the bin packing strategy humans can come up with involves around those basic ideas, it'd be nice to see if the agent end up picking the bigger boxes first, or group the boxes somehow



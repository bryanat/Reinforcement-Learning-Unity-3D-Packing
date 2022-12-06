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
  ![](screenshot of pointer network) 
  vs
  ![](screenshot of attention rnn by stanford) 
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
### Simulation must be proven in the Real
- #### Reality is the best test for Theory/Simulation
- #### Test inference in the Real with a Uhaul rental
  - #### would probably have to train a CNN for vision (package dimensions and truck dimension) so this unfortunately is a stretch/evo goal after MVP in simulation.

## Box
- ### Agent will sample spatial information of Box
  - ### size, position, mass
- ### Mass: Structural Integrity to make sure Boxes aren't crushed (w = mg)
  - #### rl environment has physics engire

## RL environment/engine
- ### Bryan is looking at RL environments to decide since Unity is off the table due to the recommended JAX over PyTorch, and Unity is highly PyTorch dependent. 
- ### researching Acme, another DeepMind library  
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

Environment and Tools
- RL Environment options:
  - Unity
  - Acme








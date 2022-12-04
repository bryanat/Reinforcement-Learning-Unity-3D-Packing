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
- ### Sequence-to-Sequence (Seq2Seq) which have been applied to language translation models can also be applied to our fitting boxes model. 
- ### Seq2Seq & Greedy https://youtu.be/wzfWHP6SXxY?t=2474
  - #### @ 41:11 putting a word in a wrong place would cause future words to be put in the wrong (non-optimal) place
  - #### @ 41:11 putting a box in a wrong place would cause future words to be put in the wrong (non-optimal) place
- ### non-Greedy algorithms > Greedy algorithms 
- ### May be able to use Decision Transformers (RL Transformer) as it approaches the problem with the reward hypothesis in reinforcement learning (the traditional RL reward hypothesis is a greedy hypothesis that says maximize the reward, where instead may want to consider optimality as desired reward or desired reward state) https://huggingface.co/docs/transformers/model_doc/decision_transformer

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

## plotly
- ### showcase the evolution and dynamics of the agent's policy
- ### because the optimal answer may not be known beforehand by the human, (which is a benefit of DRL as the human can learn the unknown from the agent), we want to use graphs to show how the agent approached local optima and global optima


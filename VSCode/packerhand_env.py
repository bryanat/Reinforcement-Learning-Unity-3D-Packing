import gym

import baselines.ppo2.ppo2 as ppo2
from baselines import logger

from mlagents_envs.environment import UnityEnvironment
from mlagents_envs.envs.unity_gym_env import UnityToGymWrapper


def main():
  unity_env = UnityEnvironment("/home/yueqi/DRL/UnityBox5/DRL-RNN-LSTM-BOX-SIM/env/boxpackingforgym001")
  env = UnityToGymWrapper(unity_env, uint8_visual=True, allow_multiple_obs=True)
  #logger.configure('./logs')  # Change to log in a different directory
  act = ppo2.learn(
    env = env,
    network = "lstm",
    total_timesteps=100000,
    lr=1e-3,
  )
  print("Saving model to unity_model.pkl")
  act.save("unity_model.pkl")


if __name__ == '__main__':
  main()

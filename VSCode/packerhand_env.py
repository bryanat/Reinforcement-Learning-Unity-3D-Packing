import gym
from mlagents_envs.environment import UnityEnvironment
from mlagents_envs.envs.unity_gym_env import UnityToGymWrapper


def main():
  unity_env = UnityEnvironment("env/boxpacking001")
  env = UnityToGymWrapper(unity_env, uint8_visual=True)
  
  
  

if __name__ == '__main__':
  main()


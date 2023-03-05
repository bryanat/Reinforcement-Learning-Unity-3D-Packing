import gym

# import baselines.ppo2.ppo2 as ppo2
# from baselines import logger

from mlagents_envs.environment import UnityEnvironment
from mlagents_envs.envs.unity_gym_env import UnityToGymWrapper
import numpy as np
import collections
import pickle
import sys
sys.path.insert(0, "/home/yueqi/DRL/D4RL/d4rl")
import d4rl




def main():
	unity_env = UnityEnvironment("/home/yueqi/DRL/UnityBox5/DRL-RNN-LSTM-BOX-SIM/env/boxpackingforgym001")
	env = UnityToGymWrapper(unity_env, uint8_visual=True, allow_multiple_obs=True)
 # Collect some data from the environment
	data = []
	for i in range(100):
		obs = env.reset()
		done = False
		while not done:
			action = env.action_space.sample()
			obs, reward, done, info = env.step(action)
			data.append((obs, action, reward))
  #logger.configure('./logs')  # Change to log in a different directory
#   act = ppo2.learn(
#     env = env,
#     network = "lstm",
#     total_timesteps=100000,
#     lr=1e-3,
#   )
#   print("Saving model to unity_model.pkl")
#   act.save("unity_model.pkl")
	# datasets = []

# for env_name in ['halfcheetah', 'hopper', 'walker2d']:
# 	for dataset_type in ['medium', 'medium-replay', 'expert']:
# 		name = f'{env_name}-{dataset_type}-v2'
		# env = gym.make(name)
	# assert isinstance(env.reset(), np.ndarray)
	# dataset = env.get_dataset()

	# N = dataset['rewards'].shape[0]
	# data_ = collections.defaultdict(list)

	# use_timeouts = False
	# if 'timeouts' in dataset:
	# 	use_timeouts = True

	# episode_step = 0
	# paths = []
	# for i in range(N):
	# 	done_bool = bool(dataset['terminals'][i])
	# 	if use_timeouts:
	# 		final_timestep = dataset['timeouts'][i]
	# 	else:
	# 		final_timestep = (episode_step == 1000-1)
	# 	for k in ['observations', 'next_observations', 'actions', 'rewards', 'terminals']:
	# 		data_[k].append(dataset[k][i])
	# 	if done_bool or final_timestep:
	# 		episode_step = 0
	# 		episode_data = {}
	# 		for k in data_:
	# 			episode_data[k] = np.array(data_[k])
	# 		paths.append(episode_data)
	# 		data_ = collections.defaultdict(list)
	# 	episode_step += 1

	# returns = np.array([np.sum(p['rewards']) for p in paths])
	# num_samples = np.sum([p['rewards'].shape[0] for p in paths])
	# print(f'Number of samples collected: {num_samples}')
	# print(f'Trajectory returns: mean = {np.mean(returns)}, std = {np.std(returns)}, max = {np.max(returns)}, min = {np.min(returns)}')

	with open('boxpakcking.pkl', 'wb') as f:
		pickle.dump("gym_data.pickle", f)


if __name__ == '__main__':
  main()

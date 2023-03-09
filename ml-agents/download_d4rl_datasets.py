import gym
import numpy as np

import collections
import pickle

import d4rl

# ml-agents imports for UnityToGymWrapper
from mlagents_envs.environment import UnityEnvironment
from mlagents_envs.envs.unity_gym_env import UnityToGymWrapper

from gym.envs.registration import register

register(
    id='yuojjgl-v1',
    # entry_point='envs:PackHandEnv',
    # max_episode_steps=300,
)


datasets = []

# for env_name in ['halfcheetah', 'hopper', 'walker2d']:
# 	for dataset_type in ['medium', 'medium-replay', 'expert']:
for env_name in ['packer']:
	for dataset_type in ['medium']:
		name = f'{env_name}-{dataset_type}-v2'

		unity_env = UnityEnvironment("/home/yueqi/DRL/UnityBox5/DRL-RNN-LSTM-BOX-SIM/ml-agents/env/multibuild01/boxpackingmulti001")
		# gym_env = UnityToGymWrapper(unity_env, uint8_visual=True, allow_multiple_obs=True) # make gym env from Unity env
		# env = gym.make(name)

		# print(type(env))
		gym_env = gym.make("yuojjgl-v1", env=UnityToGymWrapper(unity_env))
  
  		# Convert the Unity environment to a Gym environment
		# gym_env = gym.wrappers.TimeLimit(unity_env, max_episode_steps=100)
		dataset = gym_env.get_dataset() # D4RL.get_dataset()

		N = dataset['rewards'].shape[0]
		data_ = collections.defaultdict(list)

		use_timeouts = False
		if 'timeouts' in dataset:
			use_timeouts = True

		episode_step = 0
		paths = []
		for i in range(N):
			done_bool = bool(dataset['terminals'][i])
			if use_timeouts:
				final_timestep = dataset['timeouts'][i]
			else:
				final_timestep = (episode_step == 1000-1)
			for k in ['observations', 'next_observations', 'actions', 'rewards', 'terminals']:
				data_[k].append(dataset[k][i])
			if done_bool or final_timestep:
				episode_step = 0
				episode_data = {}
				for k in data_:
					episode_data[k] = np.array(data_[k])
				paths.append(episode_data)
				data_ = collections.defaultdict(list)
			episode_step += 1

		returns = np.array([np.sum(p['rewards']) for p in paths])
		num_samples = np.sum([p['rewards'].shape[0] for p in paths])
		print(f'Number of samples collected: {num_samples}')
		print(f'Trajectory returns: mean = {np.mean(returns)}, std = {np.std(returns)}, max = {np.max(returns)}, min = {np.min(returns)}')

		with open(f'{name}.pkl', 'wb') as f:
			pickle.dump(paths, f)

# DOCKER FILE FOR g5 INSTANCE
# note: run from repo root

# Start from python 3.9.13 base image
FROM python:3.9
# note: possibly start FROM ami-dl-ubuntu-20 image

# Set working directory to root dir
WORKDIR .

# Add conda with python 3.9
RUN apt-get update && \
    apt-get install -y wget && \
    wget https://repo.anaconda.com/miniconda/Miniconda3-py39_23.1.0-1-Linux-x86_64.sh && \
    sh Miniconda3-py39_23.1.0-1-Linux-x86_64.sh -bfp /usr/local && \
    rm Miniconda3-py39_23.1.0-1-Linux-x86_64.sh && \
    conda init && \
    exec $SHELL

RUN conda list && echo $WUT

# Copy the env file to the container root dir
COPY environment.yaml .

# Create the conda env and remove unused packages
RUN conda env create -f environment.yaml && \
    conda clean --all --yes

# Create the Unity directory
RUN mkdir -p /home/ubuntu/Unity

RUN conda list && echo $WUT

# Download and extract ml-agents release 20
RUN cd /home/ubuntu/Unity && \
    curl -L https://github.com/Unity-Technologies/ml-agents/archive/refs/tags/release_20.tar.gz | tar xz
# note: could possibly COPY mlagents from repo instead of download

# Install ml-agents-envs and ml-agents
RUN pip3 install -e /home/ubuntu/Unity/ml-agents-release_20/ml-agents-envs && \
    pip3 install -e /home/ubuntu/Unity/ml-agents-release_20/ml-agents

RUN chmodpath=builds/$MY_ENV/$MY_ENV.x86_64

# Copy build to container root dir
COPY builds/$MY_ENV/ .

# Give Unity build permission to be executable
RUN chmod +x $chmodpath

# Copy hyperparameter file to container
COPY Assets/ML-Agents/packerhand/Models/HandPPO_curriculum.yaml .
# note: can replace HandPPO_curriculum.yaml with $MY_HYPERPARAM variable later

# Command to run mlagents-learn
CMD ["mlagents-learn", "HandPPO_curriculum.yaml", "--env=builds/$MY_ENV/$MY_ENV", "--run-id=$MY_ENV", "--no-graphics"]

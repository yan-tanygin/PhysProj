cd $JENKINS_HOME
eval "$(ssh-agent)"
ssh-add physproj-home-app
cd $WORKSPACE/envs/home/srv-app
sh './docker-compose-up.sh'
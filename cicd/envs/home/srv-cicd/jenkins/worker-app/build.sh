cd $WORKSPACE/src/PhysProj/Phys.Lib.App
version=`date -u +%Y.%m.%d.%H%M`
dotnet publish --output $WORKSPACE/cicd/envs/home/srv-app/app/bin /p:Version=$version
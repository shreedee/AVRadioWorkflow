To compile/run we need

a) node v10.15.3
b) npm  v3.10.7
c) docker (set to run linux containers)
d) TypeWriter Visual studio extension

To get to the required version of Node and NPM we would have to downgrade
Make sure to downgrade NODE before you downgrade NPM. else npm will have issues downgrading node   
npm install -g node@10.15.3
npm install -g npm@3.10.7


#after getting the project for the first time (or after getting updates ) from SVN
a) remeber to do npm install
b) create the ClientApp/generated folder
c) slighly chage the model.tst file so that models are generated

# To Build the client side code (with watch)
node  --max_old_space_size=8192 node_modules/webpack/bin/webpack.js --mode=development --watch

To save dependencies run npm-shrinkwrap

To start the db stack 
a) in the command prompt 
b) cd to solution root, and run
c) docker-compose up

WE will have to create and configure the minio bucket to do that

on ubuntu box. make sure minio por tis allowed via firewall and 172.17.0.1 is the host address
mc config host add minio http://172.17.0.1:9000 DCFO7M7ZJR4Q681U2DUH PatXGqpsPyqvgpMd+1w+S3mD6HMiq8mfTxPz8jbD

docker run -it --rm --entrypoint=/bin/sh minio/mc

mc config host add minio http://host.docker.internal:9000 DCFO7M7ZJR4Q681U2DUH PatXGqpsPyqvgpMd+1w+S3mD6HMiq8mfTxPz8jbD
mc mb minio/revstorage
mc policy set download minio/revstorage


//want it:

final pics we want 4:3 aspect ratio.
min size : 800 X 600


image,
original
media, everything else

in original put mp3, mp4, wav, aiff, mpeg

media: everything else

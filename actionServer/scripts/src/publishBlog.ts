import * as _ from 'lodash';
import * as fs from 'fs-extra';
import * as path from 'path';
const sanitize = require("sanitize-filename");

import {FolderDetailsModel} from '../generated/FolderDetailsModel';

import fetch from 'node-fetch';
import { PublishStatusModel } from '../generated/PublishStatusModel';

import {ncp} from 'ncp';

// npm run debug -- ./src/publishBlog.ts "RadioWorkflow/2020_05_25_Interview_Sea_ploggers_English"

async function main(){
    console.log('process.argv', process.argv);
    debugger;

    if(process.argv.length<3){
        throw 'Usage node publishBlog.ts WORKFLOWFOLDR_RELATVIVEtoPLAYGROUND';
    }


    const configdata = JSON.parse( String(fs.readFileSync('./appsettings.json')));

    const wfFolder = path.join('/playground',process.argv[2]);
    console.log(`processing folder ${wfFolder}`);

    const baseFolderName = path.basename(wfFolder);

    const sesssioFileanem = `${wfFolder}/sessionData.json`;

    console.log(`using session file name ${sesssioFileanem}`);

    const wfDataStr = String(fs.readFileSync(sesssioFileanem));
    if(!wfDataStr){
        throw 'sessionData.json is not there';
    }

    const folderDetails:FolderDetailsModel = JSON.parse(wfDataStr);

    if(!folderDetails){
        throw 'failed to load folderDetails';
    }

    const {publishDetails, publishedActions} = folderDetails;

    const mediaToCopy = (publishDetails && publishDetails.mediaFiles 
        && _.filter(publishDetails.mediaFiles, f=>f.canPublish && !f.doNotPublish && !f.wpPostId)) || [];

    const published =publishedActions &&  _.find(publishedActions,p=>!!p.wpPostId);

    console.log(published?`using postId ${published.wpPostId}`:'1st time publishing');


    //read by the pho script to actiually do the publish
    const publishData ={
        category:publishDetails.category,
        author:publishDetails.programBy,
        twiterTitle:null,
        post:{
            ID:published?published.wpPostId:'',
            post_title:publishDetails.title,
            post_content:publishDetails.bodyText
        },
        attachments:_.map(mediaToCopy,m=>({
            path:m.path, //This file identited by the path

            fileName:sanitize(`${baseFolderName}_${m.fileName}`),
            post_title:m.fileName,
            isImage:m.fileType=='Images'
        }))
    };

    if(publishDetails.twiterTitle){
        publishData.twiterTitle=publishDetails.twiterTitle
    }

    if(!process.env.AVRADIO_SSHUSER){
        throw 'AVRADIO_SSHUSER needs to be set in the environment';
    }

    if(!fs.existsSync('/root/.ssh/id_rsa_toCopy')){
        throw 'ssh private key is needed in /root/.ssh/id_rsa_toCopy'
    }

    if(!process.env.AVRADIO_PUBLISHMONITOR){
        throw 'AVRADIO_PUBLISHMONITOR needs to be set to monitor folder for AVUpdaterPlugin (example /wpapp/data/pmon)';
    }

   
    const publishFileSource = path.join(wfFolder,'publishData.json');
    

    fs.writeFileSync(publishFileSource,JSON.stringify(publishData,null, "\t"));

    const filesToUpload = mediaToCopy.map(m=>({src :path.join(wfFolder,m.path),dest : path.join(process.env.AVRADIO_PUBLISHMONITOR,baseFolderName,m.path)}))
    .concat({src:publishFileSource,dest:path.join(process.env.AVRADIO_PUBLISHMONITOR,path.join(baseFolderName,'publishData.json'))});


    
    const scp2 = require('scp2');

    const scpDeuatls= {
        host:'aurovilleradio.org',
        port: 22,
        username:process.env.AVRADIO_SSHUSER,
        privateKey: String(fs.readFileSync('/root/.ssh/id_rsa_toCopy'))
    };
    
    const scpClient = new scp2.Client(scpDeuatls);
    //donot do all in parallele it always fails so        
    //await Promise.all( filesToUpload.map(async f=>{
    for (const f of filesToUpload) {
        
        console.log(`uploading ${f.src}->${f.dest}`);

        //all these uploads puts quite a bit of load so we ned to slow it down

        await new Promise( (resolve,reject)=>scpClient.upload(f.src,f.dest,err=>{
            if(err){
                reject(`failed to upload ${sesssioFileanem} . error ${JSON.stringify(err)}`);
                return;
            }
            resolve();
        }));

        console.log(`upload completed ${f.src}->${f.dest}`);
    }
    
    //}));  
    
    
    
    const updaterUri = `https://www.aurovilleradio.org/wp-json/avupdater/v1/fromfile?baseFolderName=${encodeURIComponent(baseFolderName)}`;

    console.log(`Executing ${updaterUri}`);

    const publishDone:UpdaterReposnce = await(await fetch(updaterUri)).json();

    if(!publishDone || !publishDone.success){
        throw 'failed to publish' + (publishDone.error &&  JSON.stringify(publishDone.error));
    }

    console.log(`sucessfully ${publishDone.action} -> ${publishDone.postID}`);

    folderDetails.publishedActions = (folderDetails.publishedActions||[]).concat({
        message:`${publishDone.action} -> ${publishDone.postID}`,
        wpLink:null,
        wpPostId:publishDone.postID,
        lastModified:new Date()
    });

    if(publishDone.updatedAttachments && publishDone.updatedAttachments.length>0){
        publishDone.updatedAttachments.forEach(a=>{
            const found = folderDetails.publishDetails.mediaFiles.find(f=>f.path == a.path);
            if(!found){
                console.error(`uploaded file {a.uploadPath} not found.. Why was it uploaded`);
                return;
            }

            found.wpPostId = a.ID;
            found.wpPath=a.wpPath;
        });
    }

    folderDetails.publishStatus = PublishStatusModel.publishCompleted;

    fs.writeFileSync(sesssioFileanem,JSON.stringify(folderDetails,null,'\t'));

    console.log(`Updated session file ${sesssioFileanem}`);

    const moveLocations:string[]=[];
    while(!!(process.env[`AVRADIO_AFTERPUB_FOLDER_${moveLocations.length}`])){
        moveLocations.push(path.join(
            process.env[`AVRADIO_AFTERPUB_FOLDER_${moveLocations.length}`],
            baseFolderName));
    }

    await Promise.all(moveLocations.map(async location=>{
        console.log(`saving folder ${wfFolder} to -> ${location}`);

        await new Promise((resolve,reject)=>ncp(wfFolder,location,err=>{
            if(err){
                reject(err);
                console.error(`failed to save to ${location}`, err);
            }

            resolve();
        }));

        console.log(`done copying to ${location}`);
    }));

    console.log(`Removing ${wfFolder}`);

    fs.emptydirSync(wfFolder);
    fs.removeSync(wfFolder);
    
    console.log(`All is done`);
}


type UpdaterReposnce ={
    action:string,
    postID?:number;
    success?:boolean;
    failed?:boolean;
    error?:any,
    updatedAttachments?:{
        ID:string,
        path:string,
        post_mime_type:string,
        wpPath:string,
        attach_data:[]
    }[]
};


Promise.resolve().then(async()=>{
    try
    {
        await main();
        console.log('All done');
        process.exit(0);
    }
    catch(err){
        console.error(`exception :${err}`);
        process.exit(-1);
    }
});
import * as _ from 'lodash';
import * as fs from 'fs';

async function main(){
    console.log('process.argv', process.argv);
    debugger;

    const configdata = String(fs.readFileSync('./appsettings.json'));
    
    
    const inData = [1,2,3,4,6];
    
    const added = _.reduce(inData, (acc,i)=>acc+=i,0 );
    
    console.log(`All is well we added the values to ${added}`);
}


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






async function main(){
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
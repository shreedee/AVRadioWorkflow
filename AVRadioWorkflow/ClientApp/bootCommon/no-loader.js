module.exports = function (content) {
    
    console.log('no-loading components');
    this.cacheable && this.cacheable();
    this.value = content;
    return 'export default {} as any';
}
module.exports.seperable = true
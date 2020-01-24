//async loads a components
import * as React from 'react';
import Spinner from './spinner/index';

type ViewProps = {
    loader: (resolve:any) => any,
    prompt:string,
    childProps?:any
}

export class SplitPoint extends React.Component<ViewProps, { displayer:any}> {
    constructor(props) {
        super(props);
 
        this.state = {
            displayer: '',
        };
    }

    componentDidMount(){
        let me = this;
        new Promise(resolve => {this.props.loader(resolve)})
        .then(function(component){
            me.setState({displayer:component});
        });
        
    }

    render() {
        let IShow = this.state.displayer;
        let prompt = this.props.prompt||'Loading...';
        const childProps = this.props.childProps||{};
        
        return (
            <Spinner isLoading={!this.state.displayer} prompt={prompt}>
                <IShow {...childProps}/>
            </Spinner>
            
        );
    }
    
}

//class TypedEntry  extends SplitPointT<any>{};


export default SplitPoint ;
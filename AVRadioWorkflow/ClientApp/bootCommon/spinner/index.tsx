import * as React from "react";
import './spinner.scss';

interface ISpinnerProps {
    children?: any;
    prompt: string;
    isLoading: boolean;
    style?: any;
}

const Spinner: React.SFC<ISpinnerProps> = ({ children, prompt, isLoading, style}) => {
    
    return (
        <div className="spinnerHolder" style={style || {}}>
        {isLoading?
        <div style={{textAlign:'center'}}>
            <div style={{position: 'relative',height: '60px'}}>
                <div style={{height: '100%',position: 'absolute',    width: '100%'}}>
                    <div className="loader">Loading...</div>
                </div>
            </div>
            <small className="text-info">{prompt}</small>
        </div>
        :children}
    </div>
        );
}


export default Spinner;

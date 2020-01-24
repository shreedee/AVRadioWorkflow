import * as React from "react";

import Spinner from './spinner';
import { IAsyncResult } from './asyncStateMiddleware';

interface IAsyncLoaderProps<T> {
    children: any; //PropTypes.node;
    prompt: string;
    asyncResult: IAsyncResult<T>;
    style?: any;
}

export function checkFetchError(response) {
    
    if (!response.ok) {
        
        if(!response.headers)
            console.error('checkFetchError called with non http response');

        const contentType = response.headers.get('content-type');
        if (contentType && contentType.indexOf('application/json') != -1)
            return response.json()
                .then(err => {
                    throw err;
                });
        else
            throw response.statusText;

    }
    else
        return response;
}

export const AsyncError :React.SFC<{prompt:string, asyncResult: IAsyncResult<any>}> = ({asyncResult, prompt}) =>{

    return (asyncResult && asyncResult.error)?
                    <strong className="text-danger">
                        Failed {prompt + ' - '}
                        {asyncResult && (asyncResult.error.Message ? asyncResult.error.Message : asyncResult.error.toString())}
                    </strong>:null
}

//used to create save buttons
export function createSaverView<T>() {
    const Loader: React.SFC<IAsyncLoaderProps<T>> = ({ children, asyncResult, prompt, style }) => {
        return (
            <Spinner isLoading={asyncResult && !!asyncResult.isLoading} prompt={prompt + '...'} style={style}>
                <AsyncError prompt={prompt} asyncResult={asyncResult}/>

                <div>{children}</div>
            </Spinner>
        );
    };

    return Loader;
}


export function createLoaderView<T>() {
    const Loader: React.SFC<IAsyncLoaderProps<T>> = ({ children, asyncResult, prompt, style } ) => {
        return (
            <Spinner isLoading={!!asyncResult.isLoading} prompt={prompt} style={style}>
                <AsyncError  prompt={prompt}  asyncResult={asyncResult}/>
                {!asyncResult.isLoading && !asyncResult.isLoaded && !asyncResult.result && !asyncResult.error &&
                    <div className="text-muted">{prompt} : NOT INITIALIZED</div>}
                {asyncResult.result && children}
            </Spinner>
            );
    };

    return Loader;
}



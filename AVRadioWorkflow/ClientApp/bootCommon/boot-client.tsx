import * as React from 'react';
import * as ReactDOM from 'react-dom';

import { Provider } from 'react-redux';
import { ConnectedRouter } from 'connected-react-router';


import configureStore from './configureStore';

import { createBrowserHistory } from 'history';
import routes from '../../ClientApp/routes';


// Create browser history to use in the Redux store
const baseUrl = document.getElementsByTagName('base')[0].getAttribute('href')!;
//const history = createBrowserHistory({ basename: baseUrl });
const history = createBrowserHistory();

// Get the application-wide store instance, prepopulating with state from the server where available.
const initialState = (window as any).initialReduxState as any;
const store = configureStore(history, initialState);

function renderApp() {
    // This code starts up the React app when it runs in a browser. It sets up the routing configuration
    // and injects the app into a DOM element.
    //hydrate is not in types so have to do this to avoid compilation error
    (ReactDOM as any).hydrate(
        
            <Provider store={ store }>
                <ConnectedRouter history={history} children={routes } />
            </Provider>
        ,
        document.getElementById('react-app')
    );
}

renderApp();



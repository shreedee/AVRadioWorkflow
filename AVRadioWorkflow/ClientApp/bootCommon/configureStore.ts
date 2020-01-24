import { applyMiddleware, compose, Store, StoreEnhancerStoreCreator, ReducersMapObject/*, GenericStoreEnhancer*/ } from 'redux';
import thunk from 'redux-thunk';
import { connectRouter, routerMiddleware } from 'connected-react-router';
import { History } from 'history';

import { createInjectableStore } from './baseReducer';

import promiseMiddleware from 'redux-promise';
import { asyncStateResult, asyncStateCreater } from './asyncStateMiddleware';
//import createDebounce from 'redux-debounced';


function testPersistance({ getState }) {
    return (next) =>
        (action) => {

            const prevState = getState();
            const returnValue = next(action);
            const nextState = getState();
            const actionType = String(action.type);
            const message = 'action ${actionType}';

            
            
            return returnValue;
        };
}



export default function configureStore(history: History, initialState?: any) {
    // Build middleware. These are functions that can process the actions before they reach the store.
    const windowIfDefined = typeof window === 'undefined' ? null : window as any;
    // If devTools is installed, connect to it
   // const devToolsExtension = windowIfDefined && windowIfDefined.__REDUX_DEVTOOLS_EXTENSION__ as () => GenericStoreEnhancer;

    const createStoreWithMiddleware = compose<any>(
        applyMiddleware(
            asyncStateCreater,
            promiseMiddleware,
            //createDebounce(),
            thunk,
            asyncStateResult,
            //testPersistance,
            routerMiddleware(history)
        )
        //,devToolsExtension ? devToolsExtension() : <S>(next: StoreEnhancerStoreCreator<S>) => next
    )(createInjectableStore);

    // Combine all reducers and instantiate the app-wide store instance
    const allReducers = buildRootReducer(history);

    return createStoreWithMiddleware(allReducers as any, initialState);

}

function buildRootReducer(history) {
    //return combineReducers<any>(Object.assign({}, allReducers, { routing: routerReducer }));
    return ({ router: connectRouter(history) });
}

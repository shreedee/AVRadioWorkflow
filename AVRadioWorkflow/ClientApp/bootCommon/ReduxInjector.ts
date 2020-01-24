import { createStore, combineReducers } from 'redux';
import { set, has } from 'lodash';

let _store:any = {};
let combine = combineReducers;

function combineReducersRecurse(reducers) {
  // If this is a leaf or already combined.
  if (typeof reducers === 'function') {
    return reducers;
  }

  // If this is an object of functions, combine reducers.
  if (typeof reducers === 'object') {
      var combinedReducers = {};

      Object.keys(reducers).forEach(function (key) {
          combinedReducers[key] = combineReducersRecurse(reducers[key]);
      });

      return combine(combinedReducers);
  }

  // If we get here we have an invalid item in the reducer path.
  throw new Error({
    message: 'Invalid item in reducer tree',
    item: reducers
  } as any);
}

export function createInjectStore(initialReducers, ...args) {
    

    /* Dee:- Not sure where we would use these overrides. This block is killing the initial State
  // If last item is an object, it is overrides.
    if (typeof args[args.length - 1] === 'object') {
        const overrides = args.pop();
        // Allow overriding the combineReducers function such as with redux-immutable.
        if (overrides.hasOwnProperty('combineReducers') && typeof overrides.combineReducers === 'function') {
            combine = overrides.combineReducers;
        }
    }
    */

    _store = createStore(
        combineReducersRecurse(initialReducers),
        ...args
    );

    _store.injectedReducers = initialReducers;


    return _store;
}

export function injectReducer(key, reducer, force = false, initialActions: (dispatch, getState)=>void = null) {
    // If already set, do nothing.
    if (has(_store.injectedReducers, key) && (!force)) return;


    set(_store.injectedReducers, key, reducer);
    _store.replaceReducer(combineReducersRecurse(_store.injectedReducers));

    if (initialActions)
        initialActions(_store.dispatch, _store.getState);
}

import type { ActionReducer } from '@ngrx/store';

// Meta-reducer: wraps every reducer; here it only writes the action type to the console
export function logActions<S>(reducer: ActionReducer<S>): ActionReducer<S> {
  return (state, action) => {
    console.log(`[action] ${action.type}`);
    return reducer(state, action);
  };
}

import * as React from 'react';

import { Route, Switch, Redirect} from 'react-router-dom';
import Layout from './Layout';
import SplitPoint from './bootCommon/splitPoint';



const BadRoute = () => <h2 className="text-danger text-center">nothing found</h2>


const FolderCreator = () => <SplitPoint
    prompt="Loading folderCreator ..."
    loader={(resolve) => {
        import(/* webpackChunkName: "folderCreator" */'./components/folderCreator').then(comp => resolve(comp.default));
    }} />


const routes = <Layout>
    <Switch>
        
        {/*

        <Route path='/stand/:id' component={StandView} />

        {<Route path='/item/:standId/:itemId' component={StandItemView} />}

        <Route path='/stands/:venueId' component={ChooseStandView} />

        <Route path='/myorders/:venueId' component={MyOrdersView} />

        <Route path='/kitchen/:standId' component={PrepareFoodView} />*/}

        <Route path='/publish' component={BadRoute} />

        <Route path='/foldercreator' component={FolderCreator} />

        <Redirect exact from="/" to="/foldercreator" />
        <Route component={BadRoute} />
        
    </Switch>
</Layout>;

export default routes;

import * as React from 'react';

import './siteWide.scss';
import { Navbar, Nav, Container } from 'react-bootstrap';
import { LinkContainer } from 'react-router-bootstrap';

const MainNav: React.SFC<{}> = () => {
    return <Navbar bg="light" expand="lg">
        <LinkContainer to="/"><Navbar.Brand >AVRadio Workflow</Navbar.Brand></LinkContainer>
        <Navbar.Toggle aria-controls="basic-navbar-nav" />
        <Navbar.Collapse id="basic-navbar-nav">
            <Nav className="mr-auto">
                <LinkContainer to="/foldercreator"><Nav.Link >Folder Creator</Nav.Link></LinkContainer>
                <LinkContainer to="/publish"><Nav.Link>Publish</Nav.Link></LinkContainer>
            </Nav>

        </Navbar.Collapse>
    </Navbar>;
}

import WaitBox from './components/waitBox';


class LayoutView extends React.Component<{}, {}> {

    public render() {

        return <Container>
            <MainNav />
            <WaitBox/>
            <div className="mainContent">
                {this.props.children}
            </div>
        </Container>;
    }
}

export default LayoutView;

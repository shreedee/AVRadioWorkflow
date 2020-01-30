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
                <LinkContainer to="/publiMe"><Nav.Link>PubliMe</Nav.Link></LinkContainer>
                <LinkContainer to="/foldercreator"><Nav.Link >Create new folder</Nav.Link></LinkContainer>
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
